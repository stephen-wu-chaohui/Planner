# Planner.AI Worker

This Python worker analyzes vehicle route optimization results using Google Gemini AI and publishes insights to Firestore for consumption by the Blazor application.

## Overview

The Planner.AI worker is part of a distributed system that processes route optimization results:

1. **Planner.API** sends optimization results via SignalR to the Blazor app and writes them to Firestore (`pending_analysis` collection)
2. **Planner.AI Worker** (this project) listens to Firestore, analyzes results with Google Gemini, and writes insights to `route_insights` collection
3. **Planner.BlazorApp** listens to `route_insights` and displays insights to users in real-time

## Architecture

```
┌─────────────┐     SignalR      ┌──────────────┐
│ Planner.API ├─────────────────>│ Blazor App   │
└──────┬──────┘                  └──────▲───────┘
       │                                 │
       │ Firestore Write                 │ Firestore Listen
       │ (pending_analysis)              │ (route_insights)
       ▼                                 │
┌──────────────┐                  ┌─────┴────────┐
│  Firestore   │<────────────────>│ Planner.AI   │
│              │   Listen & Write │ Worker       │
└──────────────┘                  └──────────────┘
                                        │
                                        ▼
                                  ┌────────────┐
                                  │ Google     │
                                  │ Gemini AI  │
                                  └────────────┘
```

## Setup

### Prerequisites

- Python 3.9 or higher
- Google Cloud Project with Firestore enabled
- Firebase service account credentials
- Google Gemini API key

### Installation

1. Install dependencies:
```bash
cd src/Planner.AI
pip install -r requirements.txt
```

2. Configure environment variables:
```bash
cp .env.example .env
# Edit .env and add your credentials
```

3. Set up Firebase credentials:
   - Go to [Firebase Console](https://console.firebase.google.com/)
   - Select your project
   - Go to Project Settings > Service Accounts
   - Click "Generate New Private Key"
   - Save the JSON file and set `GOOGLE_APPLICATION_CREDENTIALS` to its path

4. Get Gemini API key:
   - Go to [Google AI Studio](https://makersuite.google.com/app/apikey)
   - Create an API key
   - Set `GEMINI_API_KEY` in your `.env` file

### Running the Worker

#### Development
```bash
python planner_ai_worker.py
```

#### Production (with systemd)
Create a systemd service file:
```ini
[Unit]
Description=Planner.AI Worker
After=network.target

[Service]
Type=simple
User=planner
WorkingDirectory=/path/to/Planner/src/Planner.AI
EnvironmentFile=/path/to/Planner/src/Planner.AI/.env
ExecStart=/usr/bin/python3 /path/to/Planner/src/Planner.AI/planner_ai_worker.py
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
```

Enable and start:
```bash
sudo systemctl enable planner-ai-worker
sudo systemctl start planner-ai-worker
```

## Configuration

### Environment Variables

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `GEMINI_API_KEY` | Yes | - | Google Gemini API key |
| `GEMINI_MODEL` | No | `gemini-2.0-flash-exp` | Gemini model to use |
| `GOOGLE_APPLICATION_CREDENTIALS` | Yes | - | Path to Firebase service account JSON |

### Firestore Collections

#### Input: `pending_analysis`
Documents with `status: "new"` containing:
- `json_payload`: Optimization result data (string or object)
- `status`: "new" (unprocessed) or "processed"
- `timestamp`: Creation timestamp

#### Output: `route_insights`
Documents written after analysis:
- `analysis`: Markdown-formatted AI analysis
- `timestamp`: Server timestamp
- `status`: "completed"
- `request_id`: Reference to original request

## How It Works

1. **Listen**: Worker subscribes to Firestore query `pending_analysis WHERE status == 'new'`
2. **Process**: When a new document arrives:
   - Extract the `json_payload`
   - Send to Google Gemini for analysis
   - Generate markdown-formatted insights
3. **Publish**: Write results to `route_insights` collection
4. **Update**: Mark original document as processed

## AI Analysis

The worker uses Google Gemini to provide:
- **Route Efficiency Summary**: 2-sentence overview of optimization quality
- **Critical Stop Identification**: The stop with highest impact on route efficiency

Analysis is formatted in markdown for rich display in the Blazor UI.

## Error Handling

- **API Failures**: Returns fallback message if Gemini API fails
- **Missing Data**: Skips documents without valid `json_payload`
- **Connection Issues**: Logs errors and continues listening
- **Graceful Shutdown**: Ctrl+C cleanly unsubscribes and exits

## Monitoring

The worker logs all activities:
- Firestore connection status
- New documents detected
- Analysis progress
- Errors and warnings

View logs:
```bash
# If running directly
python planner_ai_worker.py

# If using systemd
sudo journalctl -u planner-ai-worker -f
```

## Development

### Testing
```bash
# Install dev dependencies
pip install pytest pytest-asyncio

# Run tests
pytest
```

### Code Style
The code follows PEP 8 style guidelines with comprehensive docstrings.

## Troubleshooting

### Worker not receiving documents
- Check Firestore security rules allow read/write
- Verify `GOOGLE_APPLICATION_CREDENTIALS` is correct
- Ensure documents have `status: "new"`

### Gemini API errors
- Verify `GEMINI_API_KEY` is valid
- Check API quota and billing
- Try a different model (set `GEMINI_MODEL`)

### Import errors
- Reinstall dependencies: `pip install -r requirements.txt`
- Check Python version: `python --version` (3.9+)

## License

This project is part of the Planner application and follows the same license.
