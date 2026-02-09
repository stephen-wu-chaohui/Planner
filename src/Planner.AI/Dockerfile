FROM python:3.11-slim

# Set working directory
WORKDIR /app

# Copy requirements first for better caching
COPY requirements.txt .

# Install dependencies
RUN pip install --no-cache-dir -r requirements.txt

# Copy the worker script
COPY planner_ai_worker.py .

# Set environment variables (these will be overridden by docker-compose or runtime)
ENV PYTHONUNBUFFERED=1

# Run the worker
CMD ["python", "planner_ai_worker.py"]
