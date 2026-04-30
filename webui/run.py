"""Entry point for the Pulsar WebUI server."""

import uvicorn
from dotenv import load_dotenv

load_dotenv()

from app.config import settings

if __name__ == "__main__":
    uvicorn.run(
        "app.main:app",
        host=settings.webui_host,
        port=settings.webui_port,
        reload=True,
        log_level=settings.log_level.lower(),
    )
