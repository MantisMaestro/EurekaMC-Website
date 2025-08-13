import sqlite3
from contextlib import closing
from datetime import datetime
import yaml
from mcstatus import JavaServer
import os

# Set up basic logging to record events and errors.


def load_config():
    """Loads the configuration from a YAML file."""
    env = os.getenv("EUREKA_ENVIRONMENT")
    config_path = f"config.{env}.yaml" if env else "config.yaml"

    try:
        with open(config_path, "r") as f:
            return yaml.safe_load(f)
    except FileNotFoundError:
        return None
    except yaml.YAMLError as e:
        return None

def ping_server(ip, port):
    """Pings the server and returns a list of online players."""
    try:
        server = JavaServer(ip, port)
        status = server.status()
        # If no one is online, status.players.sample will be None. Return an empty list.
        if status.players.sample is None:
            return []
        return [(player.name + ',' + player.id) for player in status.players.sample]
    except Exception as e:
        # Catch any exception during ping and log it. Return an empty list so the script doesn't crash.
        return []


def main():
    """Main execution function."""

    config = load_config()
    if not config:
        return

    # Check for required configuration keys before proceeding.
    required_keys = ['server', 'port', 'db_connection_string']
    if not all(key in config for key in required_keys):
        return

    ping_data = ping_server(config['server'], config['port'])
    return ping_data


if __name__ == "__main__":
    main()