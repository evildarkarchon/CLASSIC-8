#!/usr/bin/env python
"""Example of using ClassicLib without PySide6/GUI dependencies.

This example demonstrates how to use ClassicLib's core functionality
without requiring PySide6 to be installed.
"""

from pathlib import Path

# Import core ClassicLib functionality (no PySide6 required)
from ClassicLib import (
    HAS_PYSIDE6,
    configure_logging,
    get_game_version,
    init_message_handler,
    logger,
    msg_error,
    msg_info,
    msg_progress_context,
    msg_success,
    msg_warning,
    open_file_with_encoding,
    yaml_settings,
)


def main():
    """Demonstrate ClassicLib usage without GUI."""
    # Initialize logging
    configure_logging(log_level="INFO")
    
    # Initialize message handler in CLI mode
    init_message_handler(is_gui_mode=False)
    
    # Check if PySide6 is available
    if HAS_PYSIDE6:
        msg_info("PySide6 is available, but we're running in CLI mode")
    else:
        msg_info("Running without PySide6 (CLI mode only)")
    
    # Example: Use message functions
    msg_info("Starting example script...")
    msg_warning("This is a warning message")
    
    # Example: Progress bar in CLI mode
    items = ["file1.txt", "file2.txt", "file3.txt", "file4.txt", "file5.txt"]
    
    with msg_progress_context("Processing files", total=len(items)) as progress:
        for i, item in enumerate(items):
            # Simulate some work
            import time
            time.sleep(0.5)
            
            progress.update(1, f"Processing {item}")
            logger.info(f"Processed {item}")
    
    msg_success("All files processed successfully!")
    
    # Example: Read a file with encoding detection
    test_file = Path("test_example.txt")
    if test_file.exists():
        try:
            content = open_file_with_encoding(test_file)
            msg_info(f"Read {len(content)} characters from {test_file}")
        except Exception as e:
            msg_error(f"Failed to read file: {e}")
    
    # Example: Work with YAML settings (if available)
    try:
        # This would work with actual YAML files in a real scenario
        msg_info("YAML settings functionality is available")
    except Exception:
        msg_warning("YAML settings not configured for this example")
    
    msg_success("Example completed!")


if __name__ == "__main__":
    main()