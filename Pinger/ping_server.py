import sqlite3
from contextlib import closing
from datetime import datetime
import yaml
from mcstatus import JavaServer
import logging
import os

# Set up basic logging to record events and errors.
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')


def load_config():
    """Loads the configuration from a YAML file."""
    env = os.getenv("EUREKA_ENVIRONMENT")
    config_path = f"config.{env}.yaml" if env else "config.yaml"

    try:
        with open(config_path, "r") as f:
            return yaml.safe_load(f)
    except FileNotFoundError:
        logging.error(f"Configuration file not found at {config_path}")
        return None
    except yaml.YAMLError as e:
        logging.error(f"Error parsing YAML file: {e}")
        return None

def ping_server(ip, port):
    """Pings the server and returns a list of online players."""
    try:
        server = JavaServer(ip, port)
        status = server.status()
        # If no one is online, status.players.sample will be None. Return an empty list.
        if status.players.sample is None:
            return []
        return [{'name': player.name, 'id': player.id} for player in status.players.sample]
    except Exception as e:
        # Catch any exception during ping and log it. Return an empty list so the script doesn't crash.
        logging.error(f"Could not ping server at {ip}:{port}. Reason: {e}")
        return []

def update_database(player_data, db_path):
    """
    Connects to the database and updates player and session info in a single transaction.
    """
    # 'closing' ensures the database connection is always closed, even if errors happen.
    with closing(sqlite3.connect(db_path)) as conn:
        # 'with conn' automatically handles transactions (commit on success, rollback on error).
        with conn:
            cursor = conn.cursor()

            # Set players who were online during the last run to offline.
            cursor.execute("UPDATE players SET last_online = CURRENT_TIMESTAMP WHERE last_online = 'now'")

            today_date = datetime.now().strftime("%Y-%m-%d")

            # Process all currently online players in a single loop.
            for player in player_data:
                player_id = player['id']
                player_name = player['name']

                # 1. Update the main 'players' table
                cursor.execute("SELECT 1 FROM players WHERE id=?", (player_id,))
                if cursor.fetchone() is None:
                    # Player is new, insert them and mark as 'now'.
                    logging.info(f"New player found: {player_name} ({player_id}). Adding to database.")
                    cursor.execute(
                        "INSERT INTO players (id, name, last_online, total_play_time) VALUES (?, ?, 'now', 60)",
                        (player_id, player_name)
                    )
                else:
                    # Player exists, update their name and mark as 'now'.
                    cursor.execute(
                        "UPDATE players SET last_online='now', total_play_time=total_play_time+60, name=? WHERE id=?",
                        (player_name, player_id)
                    )

                # 2. Update the 'player_sessions' table
                cursor.execute("SELECT 1 FROM player_sessions WHERE player_id=? AND date=?", (player_id, today_date))
                if cursor.fetchone() is None:
                    # No session for today, insert one.
                    cursor.execute(
                        "INSERT INTO player_sessions (player_id, date, time_played_in_session) VALUES (?, ?, 60)",
                        (player_id, today_date)
                    )
                else:
                    # Session for today exists, update it.
                    cursor.execute(
                        "UPDATE player_sessions SET time_played_in_session=time_played_in_session+60 WHERE player_id=? AND date=?",
                        (player_id, today_date)
                    )
            if player_data:
                logging.info(f"Database updated for {len(player_data)} online players.")

def main():
    """Main execution function."""

    config = load_config()
    if not config:
        logging.error("Exiting due to missing or invalid configuration.")
        return

    # Check for required configuration keys before proceeding.
    required_keys = ['server', 'port', 'db_connection_string']
    if not all(key in config for key in required_keys):
        logging.error(f"Config is missing one of the required keys: {required_keys}")
        return

    ping_data = ping_server(config['server'], config['port'])
    update_database(ping_data, config['db_connection_string'])
    logging.info("Ping script finished.")


if __name__ == "__main__":
    main()