#!/usr/bin/env python
"""Example of using ClassicLib with PySide6/GUI components.

This example demonstrates how to use ClassicLib's GUI functionality
when PySide6 is installed.
"""

import sys
from pathlib import Path

# First check if GUI requirements are met
from ClassicLib import check_gui_requirements

gui_available, error_msg = check_gui_requirements()
if not gui_available:
    print(f"GUI not available: {error_msg}")
    print("Please install PySide6 to run this example")
    sys.exit(1)

# Now we can safely import GUI components
from PySide6.QtWidgets import QApplication, QMainWindow, QPushButton, QVBoxLayout, QWidget

from ClassicLib import (
    init_message_handler,
    msg_error,
    msg_info,
    msg_success,
    msg_warning,
)
from ClassicLib.gui import PathInputDialog


class ExampleWindow(QMainWindow):
    """Example main window demonstrating ClassicLib GUI features."""
    
    def __init__(self):
        super().__init__()
        self.setWindowTitle("ClassicLib GUI Example")
        self.setGeometry(100, 100, 400, 300)
        
        # Initialize message handler with GUI mode
        init_message_handler(parent=self, is_gui_mode=True)
        
        # Setup UI
        central_widget = QWidget()
        self.setCentralWidget(central_widget)
        
        layout = QVBoxLayout(central_widget)
        
        # Add buttons to demonstrate different message types
        info_btn = QPushButton("Show Info Message")
        info_btn.clicked.connect(self.show_info)
        layout.addWidget(info_btn)
        
        warning_btn = QPushButton("Show Warning Message")
        warning_btn.clicked.connect(self.show_warning)
        layout.addWidget(warning_btn)
        
        error_btn = QPushButton("Show Error Message")
        error_btn.clicked.connect(self.show_error)
        layout.addWidget(error_btn)
        
        success_btn = QPushButton("Show Success Message")
        success_btn.clicked.connect(self.show_success)
        layout.addWidget(success_btn)
        
        path_btn = QPushButton("Select Path")
        path_btn.clicked.connect(self.select_path)
        layout.addWidget(path_btn)
    
    def show_info(self):
        """Show info message box."""
        msg_info("This is an information message from ClassicLib!", 
                title="Info Example",
                details="Additional details can be shown here.")
    
    def show_warning(self):
        """Show warning message box."""
        msg_warning("This is a warning message!", 
                   title="Warning Example")
    
    def show_error(self):
        """Show error message box."""
        msg_error("This is an error message!", 
                 title="Error Example",
                 details="Error details would appear here.")
    
    def show_success(self):
        """Show success message box."""
        msg_success("Operation completed successfully!", 
                   title="Success Example")
    
    def select_path(self):
        """Show path selection dialog."""
        dialog = PathInputDialog(self)
        dialog.setWindowTitle("Select a Path")
        
        if dialog.exec():
            selected_path = dialog.get_path()
            msg_info(f"You selected: {selected_path}")


def main():
    """Run the GUI example."""
    app = QApplication(sys.argv)
    
    window = ExampleWindow()
    window.show()
    
    sys.exit(app.exec())


if __name__ == "__main__":
    main()