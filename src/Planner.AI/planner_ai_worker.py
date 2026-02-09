#!/usr/bin/env python3
"""
Planner.AI Worker - Route Insights Analysis
============================================

This worker listens to Firestore for new optimization results in the 'pending_analysis' collection,
analyzes them using Google Gemini AI, and writes insights to the 'route_insights' collection.

The Blazor application listens to 'route_insights' and displays the analysis to users.
"""

import os
import sys
import time
import base64
from dotenv import load_dotenv # Only needed for local testing
import json
import logging
from datetime import datetime
from typing import Dict, Any


# Load local .env file if it exists (useful for local development)
load_dotenv()

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    handlers=[
        logging.StreamHandler(sys.stdout)
    ]
)
logger = logging.getLogger(__name__)

try:
    import firebase_admin
    from firebase_admin import credentials, firestore
except ImportError as e:
    logger.error(f"Missing required dependencies: {e}")
    logger.error("Please install with: pip install -r requirements.txt")
    sys.exit(1)


class PlannerAIWorker:
    """Main worker class for processing optimization results."""
    
    def __init__(self):
        """Initialize the worker with Firestore and Gemini configurations."""
        self.db = None
        self.model = None
        self._initialize_firestore()
        self._initialize_gemini()
        logger.info("Planner.AI Worker initialized successfully")
    
    def _initialize_firestore(self):
        """Initialize Firebase Admin SDK and Firestore client."""
        try:
            # Check if Firebase is already initialized
            try:
                firebase_admin.get_app()
                logger.info("Firebase already initialized")
            except ValueError:
                # Initialize Firebase Admin SDK
                # Get the config from environment variable
                firebase_config_raw = os.environ.get('FIREBASE_CONFIG_JSON')
                
                if not firebase_config_raw:
                    logger.error("FIREBASE_CONFIG_JSON environment variable not set.")
                    return
                
                try:
                    # 1. Determine if the string is Base64 or Raw JSON
                    if not firebase_config_raw.strip().startswith('{'):
                        # It's Base64, so decode it
                        print("Decoding Firebase config from Base64...")
                        decoded_bytes = base64.b64decode(firebase_config_raw)
                        config_dict = json.loads(decoded_bytes.decode('utf-8'))
                    else:
                        # It's raw JSON string
                        print("Parsing raw Firebase JSON string...")
                        config_dict = json.loads(firebase_config_raw)

                    # 2. Initialize the SDK using the dictionary
                    cred = credentials.Certificate(config_dict)
                    firebase_admin.initialize_app(cred)
                    print("Firebase Admin SDK initialized successfully.")
        
                except Exception as e:
                    print(f"Failed to initialize Firebase: {e}")
                    
            # Get Firestore client
            self.db = firestore.client()
            logger.info("Firestore client initialized")
            
        except Exception as e:
            logger.error(f"Failed to initialize Firestore: {e}")
            raise
    
    def _initialize_gemini(self):
        """Initialize Google Gemini AI model."""
        try:
            # Configure the Gemini API
            from google import genai
            self.client = genai.Client() # It will automatically use the GEMINI_API_KEY from environment variables
            # Initialize the model (gemini-2.0-flash-exp or gemini-2.0-flash)
            self.model_name = os.getenv('GEMINI_MODEL', 'gemini-2.5-flash')
            response = self.client.models.generate_content(
                model=self.model_name, 
                contents="Hello world"
            )
            response.text  # Just to confirm the model is working
            logger.info(f"Gemini AI model '{self.model_name}' initialized")
            
        except Exception as e:
            logger.error(f"Failed to initialize Gemini: {e}")
            raise
    
    def _construct_analysis_prompt(self, json_payload: str) -> str:
        """
        Construct the prompt for Gemini AI analysis.
        
        Args:
            json_payload: JSON string of the optimization result
            
        Returns:
            Formatted prompt string
        """
        return f"""Analyze the following vehicle routing optimization result:

{json_payload}

Please provide:
1. A 2-sentence summary of the route efficiency and optimization quality
2. Identify the most critical stop for the driver (the stop with highest impact on route efficiency)

Format your response in markdown with clear sections."""
    
    def _analyze_with_gemini(self, json_payload: str) -> str:
        """
        Analyze optimization result using Gemini AI.
        
        Args:
            json_payload: JSON string of the optimization result
            
        Returns:
            Analysis text from Gemini
        """
        try:
            prompt = self._construct_analysis_prompt(json_payload)
            response = self.client.models.generate_content(
                model=self.model_name,
                contents=prompt
            )
            return response.text
            
        except Exception as e:
            logger.error(f"Gemini API error: {e}")
            # Return a fallback message if AI analysis fails
            return f"## Analysis Unavailable\n\nUnable to generate AI analysis at this time. Error: {str(e)}"
    
    def process_optimization_result(self, doc_snapshot, changes, read_time):
        """
        Callback function for Firestore listener.
        Processes new optimization results from the pending_analysis collection.
        
        Args:
            doc_snapshot: Firestore document snapshot
            changes: List of document changes
            read_time: Timestamp when documents were read
        """
        for change in changes:
            if change.type.name == 'ADDED':
                try:
                    data = change.document.to_dict()
                    request_id = change.document.id
                    
                    logger.info(f"New optimization result found: {request_id}")
                    
                    # Extract JSON payload
                    json_payload = data.get('json_payload', '')
                    if not json_payload:
                        logger.warning(f"No json_payload found in document {request_id}")
                        continue
                    
                    # If json_payload is a dict, convert to string
                    if isinstance(json_payload, dict):
                        json_payload = json.dumps(json_payload, indent=2)
                    
                    # Analyze with Gemini
                    logger.info(f"Analyzing optimization result {request_id} with Gemini AI...")
                    analysis_text = self._analyze_with_gemini(json_payload)
                    logger.info(f"Analysis completed for {request_id}")
                    
                    # Write insight to route_insights collection
                    insight_data = {
                        'analysis': analysis_text,
                        'timestamp': firestore.SERVER_TIMESTAMP,
                        'status': 'completed',
                        'request_id': request_id
                    }
                    
                    self.db.collection('route_insights').document(request_id).set(insight_data)
                    logger.info(f"Analysis published to route_insights for {request_id}")
                    
                    # Mark the original document as processed
                    change.document.reference.update({
                        'status': 'processed',
                        'processed_at': firestore.SERVER_TIMESTAMP
                    })
                    logger.info(f"Marked document {request_id} as processed")
                    
                except Exception as e:
                    logger.error(f"Error processing document {change.document.id}: {e}", exc_info=True)
    
    def start_listening(self):
        """
        Start the Firestore listener for pending_analysis collection.
        This keeps the worker running and listening for new documents.
        """
        try:
            # Set up query for documents with status: new
            query = self.db.collection('pending_analysis').where('status', '==', 'new')
            
            # Start listening
            logger.info("Starting Firestore listener for 'pending_analysis' collection...")
            query_watch = query.on_snapshot(self.process_optimization_result)
            
            logger.info("✓ Planner.AI Worker is now listening for optimization results")
            logger.info("  Waiting for documents in 'pending_analysis' with status='new'")
            logger.info("  Press Ctrl+C to stop")
            
            # Keep the main thread alive
            try:
                while True:
                    time.sleep(1)
            except KeyboardInterrupt:
                logger.info("Shutting down Planner.AI Worker...")
                query_watch.unsubscribe()
                logger.info("Worker stopped successfully")
                
        except Exception as e:
            logger.error(f"Error starting listener: {e}", exc_info=True)
            raise


def main():
    """Main entry point for the worker."""
    logger.info("=" * 60)
    logger.info("Planner.AI Worker - Route Insights Analysis")
    logger.info("=" * 60)
    
    # Check required environment variables
    required_env_vars = ['GEMINI_API_KEY']
    missing_vars = [var for var in required_env_vars if not os.getenv(var)]
    
    if missing_vars:
        logger.error(f"Missing required environment variables: {', '.join(missing_vars)}")
        logger.error("Please set these variables or use a .env file")
        sys.exit(1)
    
    try:
        worker = PlannerAIWorker()
        worker.start_listening()
    except Exception as e:
        logger.error(f"Fatal error: {e}", exc_info=True)
        sys.exit(1)


if __name__ == '__main__':
    main()
