import os
from pydantic_settings import BaseSettings


class Settings(BaseSettings):
    webui_host: str = "0.0.0.0"
    webui_port: int = 8000
    pulsar_config_dir: str = os.path.join(os.environ.get("APPDATA", ""), "Magnetar")
    ds_install_dir: str = ""
    ds_config_dir: str = os.path.join(os.environ.get("APPDATA", ""), "SpaceEngineersDedicated")
    admin_api_url: str = "http://127.0.0.1:9000"
    log_level: str = "INFO"

    model_config = {"env_file": ".env", "env_file_encoding": "utf-8"}


settings = Settings()
