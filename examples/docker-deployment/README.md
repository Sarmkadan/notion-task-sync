# Docker Deployment Example

This example demonstrates how to deploy Notion Task Sync using Docker and Docker Compose.

## Prerequisites
- Docker and Docker Compose installed
- Notion API token
- Notion database ID

## Setup

1. Create a `.env` file with your Notion credentials:
```bash
NOTION_API_KEY=your_integration_token
NOTION_DATABASE_ID=your_database_id
```

2. Start the services:
```bash
docker-compose up -d
```

## Configuration

The docker-compose.yml file includes:
- Environment variable configuration
- Volume mapping for task persistence
- Port mapping for any web interfaces

## Environment Variables

- `NOTION_API_KEY`: Your Notion integration token
- `NOTION_DATABASE_ID`: Your Notion database ID

## Volumes

- `./tasks`: Local task files directory

## Ports

- `8080`: Web interface port (if applicable)

## Customization

You can customize this setup by modifying the docker-compose.yml file to fit your specific needs.