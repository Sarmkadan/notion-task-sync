## Docker Guide

### Quick Start
1. Install Docker on your system
2. Build the Docker image: `docker build -t notion-task-sync:latest .`
3. Run the Docker container: `docker run -e NotionApi__ApiKey=your_token -v $(pwd)/tasks:/app/tasks notion-task-sync:latest`

### Environment Variables
* `NotionApi__ApiKey`: Your Notion API token
* `LocalTasksDirectory`: The directory where your local tasks are stored

### Docker Compose
1. Create a `docker-compose.yml` file with the following content:
```yml
version: '3'
services:
  notion-task-sync:
    build: .
    environment:
      - NotionApi__ApiKey=your_token
      - LocalTasksDirectory=/app/tasks
    volumes:
      - ./tasks:/app/tasks
```
2. Run `docker-compose up -d` to start the container in detached mode

### Production Deployment Checklist
1. Configure your Notion API token and local tasks directory
2. Build and push the Docker image to your registry
3. Deploy the container to your production environment
4. Configure any additional settings as needed