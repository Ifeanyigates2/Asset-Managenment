#!/usr/bin/env python3
"""Generate FRISL RFID Asset Management System User Training Manual PDF."""

from pathlib import Path
from fpdf import FPDF


class ManualPDF(FPDF):
    def header(self):
        if self.page_no() > 1:
            self.set_font("Helvetica", "I", 8)
            self.set_text_color(100, 100, 100)
            self.cell(0, 8, "FRISL RFID Asset Management System - User Training Manual (MVP v1.0)", align="C")
            self.ln(10)

    def footer(self):
        self.set_y(-15)
        self.set_font("Helvetica", "I", 8)
        self.set_text_color(100, 100, 100)
        self.cell(0, 10, f"Page {self.page_no()}", align="C")

    def chapter_title(self, title: str):
        self.ln(4)
        self.set_x(self.l_margin)
        self.set_font("Helvetica", "B", 14)
        self.set_text_color(29, 52, 97)
        self.multi_cell(self.epw, 8, title)
        self.ln(2)

    def section_title(self, title: str):
        self.ln(2)
        self.set_x(self.l_margin)
        self.set_font("Helvetica", "B", 11)
        self.set_text_color(40, 40, 40)
        self.multi_cell(self.epw, 7, title)
        self.ln(1)

    def body_text(self, text: str):
        self.set_x(self.l_margin)
        self.set_font("Helvetica", "", 10)
        self.set_text_color(30, 30, 30)
        self.multi_cell(self.epw, 5.5, text)
        self.ln(1)

    def bullet(self, text: str):
        self.set_x(self.l_margin)
        self.set_font("Helvetica", "", 10)
        self.set_text_color(30, 30, 30)
        self.multi_cell(self.epw, 5.5, f"  - {text}")

    def account_row(self, username: str, password: str, role: str, portal: str):
        self.set_x(self.l_margin)
        self.set_font("Helvetica", "", 9)
        self.set_text_color(30, 30, 30)
        self.multi_cell(self.epw, 5, f"  {username}  |  {password}  |  {role}  |  {portal}")

    def code_block(self, text: str):
        self.set_x(self.l_margin)
        self.set_font("Courier", "", 8)
        self.set_text_color(20, 20, 20)
        self.multi_cell(self.epw, 4.5, text)
        self.ln(1)


def build_pdf(output_path: Path) -> None:
    pdf = ManualPDF()
    pdf.set_margins(20, 20, 20)
    pdf.set_auto_page_break(auto=True, margin=20)
    pdf.add_page()

    # Cover
    pdf.set_font("Helvetica", "B", 18)
    pdf.set_text_color(29, 52, 97)
    pdf.ln(30)
    pdf.cell(0, 10, "FIRST REGISTRARS & INVESTOR SERVICES LTD", ln=True, align="C")
    pdf.ln(6)
    pdf.set_font("Helvetica", "B", 16)
    pdf.cell(0, 9, "RFID ASSET MANAGEMENT SYSTEM", ln=True, align="C")
    pdf.ln(8)
    pdf.set_font("Helvetica", "B", 13)
    pdf.cell(0, 8, "USER TRAINING MANUAL (MVP VERSION)", ln=True, align="C")
    pdf.ln(20)
    pdf.set_font("Helvetica", "", 11)
    pdf.set_text_color(60, 60, 60)
    pdf.cell(0, 7, "Prepared By: Intelliverse Technologies", ln=True, align="C")
    pdf.cell(0, 7, "Version: 1.0", ln=True, align="C")

    # TOC
    pdf.add_page()
    pdf.chapter_title("TABLE OF CONTENTS")
    toc = [
        "1. Introduction",
        "2. System Overview",
        "3. User Roles",
        "4. Logging into the System",
        "5. Dashboard Overview",
        "6. Master Data Setup",
        "7. User Management",
        "8. Asset Registration",
        "9. RFID Tag Management",
        "10. Existing Asset RFID Migration",
        "11. Registering New RFID Assets",
        "12. Asset Assignment",
        "13. Asset Transfer",
        "14. RFID Stock Verification",
        "15. Asset Search",
        "16. Reports",
        "17. Audit Trail",
        "18. Best Practices",
        "19. Troubleshooting",
        "20. Practical Assessment",
        "21. Recommended RFID Readers",
        "22. Connecting Readers to FRISL EAMS",
        "23. Zebra RFD40 + TC-Series Setup Guide",
        "24. RFID API Integration (Production)",
    ]
    for item in toc:
        pdf.body_text(item)

    # Ch 1
    pdf.add_page()
    pdf.chapter_title("CHAPTER 1 - INTRODUCTION")
    pdf.section_title("Purpose")
    pdf.body_text(
        "The RFID Asset Management System enables First Registrars & Investor Services Ltd "
        "to electronically manage, track and verify all company assets using RFID technology. "
        "The system improves accountability, simplifies asset audits, reduces losses and "
        "provides real-time visibility of company assets throughout their lifecycle."
    )
    pdf.section_title("Objectives")
    pdf.body_text("At the end of this training, users should be able to:")
    for item in [
        "Navigate the application",
        "Create users (Back Office)",
        "Register assets",
        "Assign RFID tags",
        "Verify assets using handheld RFID readers",
        "Assign assets to employees",
        "Transfer assets between locations",
        "Conduct RFID stock verification",
        "Generate reports",
        "Maintain complete asset histories",
    ]:
        pdf.bullet(item)

    # Ch 2 - User Roles
    pdf.add_page()
    pdf.chapter_title("CHAPTER 2 - USER ROLES")
    pdf.section_title("Staff")
    pdf.body_text("Responsibilities:")
    for item in [
        "View assigned assets",
        "View asset details",
        "Request asset transfers",
        "Report damaged or lost assets",
    ]:
        pdf.bullet(item)

    pdf.section_title("Back Office")
    pdf.body_text("Responsible for daily asset operations. Functions:")
    for item in [
        "Create and manage users",
        "Maintain departments and locations",
        "Register assets",
        "Assign RFID tags",
        "Assign assets to staff",
        "Process transfers",
        "Conduct stock verification",
        "Generate operational reports",
    ]:
        pdf.bullet(item)

    pdf.section_title("Auditor")
    pdf.body_text("Responsible for:")
    for item in [
        "Asset verification",
        "Audit reports",
        "Compliance reviews",
        "Read-only access to operational records",
    ]:
        pdf.bullet(item)

    pdf.section_title("Administrator")
    pdf.body_text("Responsible for:")
    for item in [
        "System configuration",
        "Security settings",
        "Role and permission management",
        "RFID reader configuration",
        "Backups and recovery",
        "System maintenance",
    ]:
        pdf.bullet(item)

    # Ch 3 - Login
    pdf.add_page()
    pdf.chapter_title("CHAPTER 3 - LOGIN")
    pdf.body_text("Login URL: http://localhost:5000/Account/Login")
    pdf.section_title("Login Steps")
    for step in [
        "Enter Username",
        "Enter Password",
        "Click Login",
        "Change the default password on first login (if prompted)",
    ]:
        pdf.bullet(step)

    pdf.section_title("Default Demo Accounts")
    pdf.body_text(
        "The following accounts are pre-configured in the system for training and testing. "
        "Use the username and password shown to access the corresponding portal."
    )
    pdf.ln(1)
    pdf.set_x(pdf.l_margin)
    pdf.set_font("Helvetica", "B", 9)
    pdf.multi_cell(pdf.epw, 5, "  Username  |  Password  |  Role  |  Portal")
    pdf.set_font("Helvetica", "", 9)

    default_accounts = [
        ("Washington", "Washington1", "Backoffice", "Back Office"),
        ("Staff", "Staff1", "Staff", "Staff"),
        ("Auditor", "auditor1", "Auditor", "Auditor"),
        ("Admin", "Admin1", "Admin", "Administrator"),
        ("oludele.gbenro@firstregistrarsnigeria.com", "Password@123", "Backoffice", "Back Office"),
        ("Staff1", "January2021###", "Staff", "Staff"),
        ("Staff2", "January2021###", "Staff", "Staff"),
        ("Staff3", "January2021###", "Staff", "Staff"),
        ("Staff4", "January2021###", "Staff", "Staff"),
        ("staff", "staff123", "Staff", "Staff"),
        ("viewer", "viewer123", "Viewer", "Read-only (limited access)"),
    ]
    for username, password, role, portal in default_accounts:
        pdf.account_row(username, password, role, portal)

    pdf.ln(2)
    pdf.section_title("Recommended Training Accounts")
    pdf.body_text("For the practical assessment in Chapter 20, use these primary accounts:")
    pdf.bullet("Back Office operations: Washington / Washington1")
    pdf.bullet("Staff self-service: Staff / Staff1")
    pdf.bullet("Audit and verification: Auditor / auditor1")
    pdf.bullet("System administration: Admin / Admin1")

    # Ch 4 - Dashboard
    pdf.chapter_title("CHAPTER 4 - DASHBOARD")
    pdf.body_text("Each dashboard provides role-specific information.")
    pdf.section_title("Back Office Dashboard")
    for item in [
        "Total Assets",
        "Assets Assigned",
        "Assets Awaiting RFID",
        "Assets Without Custodian",
        "Pending Transfers",
        "Recent Activities",
    ]:
        pdf.bullet(item)

    pdf.section_title("Auditor Dashboard")
    for item in ["Verification Progress", "Audit Exceptions", "Missing Assets"]:
        pdf.bullet(item)

    pdf.section_title("Staff Dashboard")
    for item in ["My Assigned Assets", "Pending Requests"]:
        pdf.bullet(item)

    pdf.section_title("Administrator Dashboard")
    for item in ["Active Users", "RFID Readers", "System Health", "Audit Logs"]:
        pdf.bullet(item)

    # Ch 5 - Master Data
    pdf.add_page()
    pdf.chapter_title("CHAPTER 5 - MASTER DATA SETUP")
    pdf.body_text("Before registering assets, configure:")
    for item in [
        "Branches", "Buildings", "Floors", "Rooms", "Departments",
        "Asset Categories", "Asset Types", "Vendors", "Manufacturers",
    ]:
        pdf.bullet(item)
    pdf.body_text("These records ensure consistent data entry throughout the system.")

    # Ch 6 - User Management
    pdf.chapter_title("CHAPTER 6 - USER MANAGEMENT (BACK OFFICE)")
    pdf.section_title("Create a User")
    pdf.body_text("Navigate to: Administration -> Users -> Create User")
    pdf.body_text("Complete:")
    for item in [
        "Employee ID", "Name", "Email", "Department", "Branch",
        "Designation", "Phone Number", "User Role", "Temporary Password",
    ]:
        pdf.bullet(item)
    pdf.body_text("Click Save.")
    pdf.section_title("Manage Users")
    pdf.body_text("Back Office can:")
    for item in [
        "Edit user details",
        "Activate or deactivate users",
        "Reset passwords",
        "Search by Employee ID, Name or Department",
    ]:
        pdf.bullet(item)

    # Ch 7 - Asset Registration
    pdf.add_page()
    pdf.chapter_title("CHAPTER 7 - ASSET REGISTRATION")
    pdf.body_text("To register a new asset:")
    pdf.bullet("Go to Assets -> New Asset")
    pdf.body_text("Enter:")
    for item in [
        "Asset Number", "Category", "Asset Type", "Description", "Manufacturer",
        "Model", "Serial Number", "Purchase Date", "Purchase Cost", "Vendor",
        "Department", "Default Location",
    ]:
        pdf.bullet(item)
    pdf.bullet("Save the asset.")
    pdf.body_text("The asset is now recorded but is NOT yet RFID-enabled.")

    # Ch 8 - RFID Tag Management
    pdf.chapter_title("CHAPTER 8 - RFID TAG MANAGEMENT")
    pdf.body_text("RFID tags must be unique. The system supports:")
    for item in ["Assign RFID Tag", "Replace RFID Tag", "Remove RFID Tag", "Verify RFID Tag"]:
        pdf.bullet(item)
    pdf.body_text("The system prevents duplicate RFID assignments.")
    pdf.section_title("RFID Tag Statuses")
    for item in ["Blank", "Assigned", "Active", "Damaged", "Lost", "Retired"]:
        pdf.bullet(item)

    # Ch 9 - Migration
    pdf.add_page()
    pdf.chapter_title("CHAPTER 9 - EXISTING ASSET RFID MIGRATION")
    pdf.body_text("This process is used for assets that already exist in the database.")
    pdf.section_title("Workflow")
    for i, step in enumerate([
        "Search for the asset.",
        "Open the asset record.",
        "Click Assign RFID Tag.",
        "Connect the RFID handheld reader.",
        "Select Write Tag.",
        "Place a blank RFID tag on the reader.",
        "The system writes a unique EPC to the tag.",
        "The EPC is linked permanently to the selected asset.",
        "Verify the tag by scanning it.",
        "Save.",
    ], 1):
        pdf.bullet(f"{step}")
    pdf.body_text("The asset is now RFID-enabled.")

    # Ch 10 - New RFID Assets
    pdf.chapter_title("CHAPTER 10 - REGISTERING NEW RFID ASSETS")
    for i, step in enumerate([
        "Register the asset.",
        "Save the asset record.",
        "Select Assign RFID Tag.",
        "Encode a blank RFID tag.",
        "Verify the RFID tag.",
        "Mark the asset as Available.",
    ], 1):
        pdf.bullet(step)

    # Ch 11 - Handheld Reader
    pdf.add_page()
    pdf.chapter_title("CHAPTER 11 - USING THE RFID HANDHELD READER")
    pdf.body_text("The RFID reader has three primary operating modes.")

    pdf.section_title("Read Mode")
    pdf.body_text("Purpose: Identify an existing RFID-tagged asset.")
    pdf.body_text("App screen: RFID Scan -> Read Mode (/RfidReader?mode=read)")
    pdf.body_text("Steps:")
    for step in [
        "Open RFID Scan from the portal sidebar.",
        "Select Read Mode.",
        "Tap the RFID Code field.",
        "Scan the tag with the handheld reader (or type the code).",
        "Submit - asset details appear automatically.",
    ]:
        pdf.bullet(step)

    pdf.section_title("Write Mode")
    pdf.body_text("Purpose: Encode a new RFID tag and link it to an asset.")
    pdf.body_text("App screen: RFID Scan -> Write Mode (/RfidReader?mode=write) or Assets -> Assign RFID Tag")
    pdf.body_text("Steps:")
    for step in [
        "Select the asset in the system.",
        "Encode the blank tag using Zebra 123RFID Mobile (hardware) or Write RFID Tag in the app.",
        "Place a blank RFID tag on the reader.",
        "Wait for confirmation.",
        "Verify by scanning the newly encoded tag in Verification Mode.",
    ]:
        pdf.bullet(step)

    pdf.section_title("Verification Mode")
    pdf.body_text("Purpose: Confirm that an RFID tag is correctly linked to the asset.")
    pdf.body_text("App screen: RFID Scan -> Verification Mode (/RfidReader?mode=verify)")
    pdf.body_text("The system displays:")
    for item in [
        "Asset Number", "Description", "Current Custodian",
        "Department", "Current Location", "Status",
    ]:
        pdf.bullet(item)

    pdf.section_title("Unknown RFID Tags")
    pdf.body_text("If the reader detects an unknown RFID tag, the system displays:")
    pdf.body_text("Unknown RFID Tag Detected")
    pdf.body_text("Options:")
    for item in ["Register as New Asset", "Link to Existing Asset", "Ignore"]:
        pdf.bullet(item)

    # Ch 12 - Assignment
    pdf.add_page()
    pdf.chapter_title("CHAPTER 12 - ASSET ASSIGNMENT")
    for i, step in enumerate([
        "Open the asset record.",
        "Click Assign Asset.",
        "Select the employee.",
        "Scan the RFID tag for verification.",
        "Confirm the assignment.",
    ], 1):
        pdf.bullet(step)
    pdf.body_text("The employee becomes the current custodian.")

    # Ch 13 - Transfer
    pdf.chapter_title("CHAPTER 13 - ASSET TRANSFER")
    for i, step in enumerate([
        "Select the asset.",
        "Choose the destination department or location.",
        "Submit the transfer request.",
        "Upon approval, scan the asset before it leaves its current location.",
        "Scan the asset again upon arrival.",
        "Complete the transfer.",
    ], 1):
        pdf.bullet(step)
    pdf.body_text("The movement history is recorded automatically.")

    # Ch 14 - Stock Verification
    pdf.chapter_title("CHAPTER 14 - RFID STOCK VERIFICATION")
    pdf.body_text("Purpose: Verify the physical presence of assets.")
    pdf.body_text("Steps:")
    for i, step in enumerate([
        "Select a department or room.",
        "Start Stock Verification.",
        "Walk through the area with the RFID reader.",
        "The reader automatically detects tagged assets.",
        "The system compares scanned assets with expected records.",
    ], 1):
        pdf.bullet(step)
    pdf.section_title("Results")
    for item in ["Found Assets", "Missing Assets", "Unexpected Assets", "Duplicate Reads"]:
        pdf.bullet(item)
    pdf.body_text("Generate the verification report after completion.")

    # Ch 15 - Search
    pdf.add_page()
    pdf.chapter_title("CHAPTER 15 - SEARCH")
    pdf.body_text("Search by:")
    for item in [
        "Asset Number", "RFID Tag", "Serial Number", "Employee",
        "Department", "Location", "Manufacturer",
    ]:
        pdf.bullet(item)

    # Ch 16 - Reports
    pdf.chapter_title("CHAPTER 16 - REPORTS")
    pdf.body_text("Available reports include:")
    for item in [
        "Asset Register", "Employee Asset Register", "Department Asset Report",
        "RFID Assignment Report", "Assets Without RFID", "Verification Report",
        "Missing Assets", "Asset Movement Report", "Audit Report",
    ]:
        pdf.bullet(item)
    pdf.body_text("Reports can be exported to Excel or PDF.")

    # Ch 17 - Audit Trail
    pdf.chapter_title("CHAPTER 17 - AUDIT TRAIL")
    pdf.body_text("Every action performed in the system is recorded.")
    pdf.body_text("Captured information includes:")
    for item in ["User", "Action", "Date", "Time", "Previous Value", "New Value"]:
        pdf.bullet(item)
    pdf.body_text("Audit records cannot be modified or deleted.")

    # Ch 18 - Best Practices
    pdf.chapter_title("CHAPTER 18 - BEST PRACTICES")
    for item in [
        "Never share login credentials.",
        "Verify an RFID tag immediately after encoding.",
        "Scan assets before and after every transfer.",
        "Investigate unknown RFID tags promptly.",
        "Do not reuse retired RFID tags without an approved process.",
        "Perform regular stock verification.",
    ]:
        pdf.bullet(item)

    # Ch 19 - Troubleshooting
    pdf.add_page()
    pdf.chapter_title("CHAPTER 19 - TROUBLESHOOTING")
    pdf.section_title("Problem: RFID tag not detected.")
    pdf.body_text("Action:")
    for item in [
        "Confirm the reader is connected.",
        "Ensure the tag is within reading range.",
        "Verify the tag has been encoded.",
    ]:
        pdf.bullet(item)

    pdf.section_title("Problem: Duplicate RFID detected.")
    pdf.body_text("Action:")
    for item in [
        "Locate the existing asset linked to the tag.",
        "Do not assign the same RFID tag to another asset.",
    ]:
        pdf.bullet(item)

    pdf.section_title("Problem: User cannot log in.")
    pdf.body_text("Action:")
    for item in [
        "Verify the account is active.",
        "Reset the password if required.",
    ]:
        pdf.bullet(item)

    pdf.section_title("Problem: Zebra RFD40 scan not appearing in browser.")
    pdf.body_text("Action:")
    for item in [
        "Confirm the RFID input field is focused before scanning.",
        "Verify DataWedge profile is associated with Chrome browser.",
        "Check RFID Input is enabled and RFD40 is selected as reader.",
        "Update RFD40 firmware to 006-R01 or higher.",
        "Re-pair sled using Zebra Bluetooth Pairing Utility if using Bluetooth.",
    ]:
        pdf.bullet(item)

    # Ch 20 - Practical Assessment
    pdf.chapter_title("CHAPTER 20 - PRACTICAL ASSESSMENT")
    pdf.body_text("Each trainee must complete the following end-to-end exercise:")
    for i, step in enumerate([
        "Create a new staff user.",
        "Register a new laptop asset.",
        "Encode and assign an RFID tag.",
        "Verify the tag using the handheld reader.",
        "Search for an existing asset and assign it an RFID tag.",
        "Assign the new laptop to the staff user.",
        "Transfer the laptop to another department.",
        "Conduct an RFID stock verification.",
        "Generate an Asset Register report.",
        "Review the audit trail for all actions performed.",
    ], 1):
        pdf.bullet(f"{step}")
    pdf.body_text(
        "A trainee is considered competent when they can complete the full workflow "
        "independently and accurately."
    )

    # Ch 21 - Recommended RFID Readers
    pdf.add_page()
    pdf.chapter_title("CHAPTER 21 - RECOMMENDED RFID READERS")
    pdf.body_text(
        "Use UHF passive RFID tags (EPC Gen2 / ISO 18000-6C) for all asset labels. "
        "The system supports readers that output tag data via keyboard wedge (DataWedge) "
        "or via REST API integration."
    )

    pdf.section_title("Handheld UHF Readers (tagging, verification, stock take)")
    readers_handheld = [
        "Zebra RFD40 (with TC21/TC26/TC52/TC53 mobile computer) - recommended for enterprise rollout",
        "Zebra RFD90 - longer range, rugged environments",
        "Honeywell IH25 - solid handheld for inventory audits",
        "Chainway C72 / C5 - cost-effective Android handheld with built-in UHF",
        "TSL 1128 - Bluetooth UHF sled used with phone or tablet",
    ]
    for item in readers_handheld:
        pdf.bullet(item)

    pdf.section_title("Fixed Door / Gate Readers (exit monitoring)")
    for item in [
        "Impinj Speedway R420 / R700 with antennas - industry standard portal readers",
        "Zebra FX7500 / FX9600 - fixed reader for doorways and choke points",
        "ThingMagic Sargas - compact fixed reader for single-door setups",
    ]:
        pdf.bullet(item)

    pdf.section_title("Desktop USB Readers (encoding stations)")
    for item in [
        "ThingMagic USB Plus+ - desktop UHF for back-office tag encoding",
        "Generic UHF USB readers - suitable for low-volume tagging desks",
    ]:
        pdf.bullet(item)

    pdf.section_title("Tag Types")
    for item in [
        "Standard adhesive UHF labels - furniture, general equipment",
        "On-metal UHF tags - laptops, servers, metal cabinets",
        "Rugged high-memory tags - vehicles and large outdoor assets",
    ]:
        pdf.bullet(item)

    pdf.section_title("Suggested Rollout for First Registrars")
    pdf.bullet("Phase 1 (Training): Chainway C72 or Zebra RFD40 + TC26 in keyboard wedge mode")
    pdf.bullet("Phase 2 (Operations): Zebra RFD40 fleet with DataWedge profiles pushed via StageNow")
    pdf.bullet("Phase 3 (Security): Zebra FX9600 at main entrance with API middleware for exit alerts")

    # Ch 22 - Connecting Readers
    pdf.add_page()
    pdf.chapter_title("CHAPTER 22 - CONNECTING READERS TO FRISL EAMS")
    pdf.body_text(
        "FRISL EAMS is a web application. RFID readers do not plug into the server directly. "
        "They connect to a client device (PC, tablet, or Zebra mobile computer) that runs the browser "
        "or a middleware service posting scans to the API."
    )

    pdf.section_title("Connection Methods")
    pdf.body_text("Method 1 - Keyboard Wedge / DataWedge (recommended for handhelds)")
    for item in [
        "Reader sends EPC as keystrokes into the focused browser field.",
        "Works today with no custom development.",
        "Use with /RfidReader, /StockVerification, /Assignments, /Transfers.",
    ]:
        pdf.bullet(item)

    pdf.body_text("Method 2 - REST API (recommended for door readers and batch stock take)")
    for item in [
        "Middleware or custom app POSTs scan results to FRISL API endpoints.",
        "Suitable for fixed portal readers and high-volume stock verification.",
        "See Chapter 24 for API details.",
    ]:
        pdf.bullet(item)

    pdf.body_text("Method 3 - Direct USB/Bluetooth SDK in browser")
    pdf.bullet("Not supported in the current MVP web application.")

    pdf.section_title("App Screens for RFID Workflows")
    workflow_table = [
        ("Read / verify tag", "/RfidReader?mode=read or verify"),
        ("Write / assign tag", "/RfidReader?mode=write or /RfidTags/Assign"),
        ("Stock verification", "/StockVerification"),
        ("Assignment RFID confirm", "/Assignments"),
        ("Transfer departure / arrival", "/Transfers"),
        ("Exit / door monitoring", "/Workflow/Rfid"),
    ]
    for task, route in workflow_table:
        pdf.bullet(f"{task}: {route}")

    pdf.section_title("Quick Test Without Hardware")
    pdf.body_text(
        "Open /RfidReader?mode=verify, type a seeded tag such as RFID-000101, and submit. "
        "Asset details should appear if the tag is linked in the system."
    )

    # Ch 23 - Zebra RFD40 Setup
    pdf.add_page()
    pdf.chapter_title("CHAPTER 23 - ZEBRA RFD40 + TC-SERIES SETUP GUIDE")
    pdf.body_text(
        "This chapter describes how to connect the Zebra RFD40 UHF sled with a TC-series "
        "mobile computer (TC21, TC26, TC52, TC53, TC58) to FRISL EAMS."
    )

    pdf.section_title("Hardware Required")
    for item in [
        "Zebra RFD40 sled (Standard, Premium, or Premium Plus)",
        "Zebra TC21/TC26/TC52/TC53/TC58 mobile computer",
        "eConnex adapter (e.g. ADP-RFD40-TC2X-1E) for snap-on connection - recommended",
        "OR Bluetooth adapter (ADP-RFD40-TC2X-1R) for wireless connection",
        "UHF EPC Gen2 asset tags (on-metal tags for IT equipment)",
        "Note: eConnex requires TC device with 8-pin rear connector SKU",
    ]:
        pdf.bullet(item)

    pdf.section_title("Physical Connection - eConnex (Recommended)")
    for step in [
        "Attach RFD40 to the TC mobile computer using the correct eConnex adapter.",
        "Power on both the sled and the mobile computer.",
        "The sled is detected via the 8-pin connector (Bluetooth is disabled in this mode).",
        "Connect TC to Wi-Fi or mobile data.",
        "Open Chrome and navigate to the FRISL login URL.",
    ]:
        pdf.bullet(step)

    pdf.section_title("Physical Connection - Bluetooth")
    for step in [
        "Attach the Bluetooth adapter to the RFD40.",
        "Open Zebra Bluetooth Pairing Utility on the TC device.",
        "Scan the pairing barcode and tap PAIR.",
        "Configure DataWedge mode (see below).",
    ]:
        pdf.bullet(step)

    pdf.section_title("Software Prerequisites")
    for item in [
        "RFD40 firmware version 006-R01 or higher",
        "DataWedge version 13.0 or higher on the TC device",
        "123RFID Desktop (PC) or 123RFID Mobile (TC) for initial configuration",
    ]:
        pdf.bullet(item)

    pdf.section_title("Configure RFD40 for DataWedge Mode")
    for step in [
        "Connect RFD40 to PC via USB or use 123RFID Mobile on the TC.",
        "Set regulatory region (Country of Operation) for your location.",
        "Set Bluetooth Host Type = DataWedge (for Bluetooth setups).",
        "Pair sled to TC using Bluetooth Pairing Utility if applicable.",
        "On TC: open DataWedge -> create profile for Chrome browser.",
        "In profile: enable RFID Input, select RFD40 as reader.",
        "Set output to Keystroke (keyboard wedge). Optionally suffix with Enter after scan.",
    ]:
        pdf.bullet(step)

    pdf.section_title("DataWedge Profile Checklist")
    for item in [
        "Associated app: Chrome (or Zebra Enterprise Browser)",
        "RFID Input: Enabled",
        "Reader selection: RFD40",
        "Output: Keystroke",
        "Key event: Enter after scan (optional, for auto-submit workflows)",
    ]:
        pdf.bullet(item)

    pdf.section_title("Using RFD40 with FRISL EAMS")
    pdf.body_text("Login URL: http://<server>:5000/Account/Login")
    pdf.body_text("Recommended training account: Washington / Washington1 (Back Office)")
    pdf.body_text("Workflow steps on TC device:")
    for step in [
        "Sign in to FRISL EAMS in Chrome.",
        "Open the required screen (e.g. RFID Scan -> Verification Mode).",
        "Tap the RFID Code input field to focus it.",
        "Pull the RFD40 trigger and scan the asset tag.",
        "The EPC appears in the field - press Submit or Enter.",
    ]:
        pdf.bullet(step)

    pdf.section_title("Workflow Mapping")
    for item in [
        "Read / verify: /RfidReader?mode=verify - tap field, scan, submit",
        "Stock verification: /StockVerification - start session, scan each tag",
        "Assignment confirm: /Assignments - scan into verification field",
        "Transfer scans: /Transfers - scan at departure and arrival",
        "Assign tag to asset: Asset detail -> Assign RFID Tag - scan EPC",
    ]:
        pdf.bullet(item)

    pdf.section_title("Write Mode (Encode New Tags) - Important")
    pdf.body_text(
        "Real EPC encoding to blank RFID tags requires Zebra 123RFID Mobile or the Zebra RFID SDK "
        "on the TC device. The FRISL web app Write Mode simulates linking for training. "
        "Production workflow: (1) encode tag with Zebra tools, (2) link EPC in FRISL via Assign RFID Tag."
    )

    pdf.section_title("Architecture Overview")
    pdf.code_block(
        "RFD40 sled --(DataWedge keystroke)--> TC26 Chrome --> FRISL EAMS\n"
        "                         /RfidReader, /StockVerification, /Assignments"
    )

    pdf.section_title("Zebra RFD40 Troubleshooting")
    for item in [
        "No scan in browser: confirm RFID field is focused; check DataWedge profile for Chrome",
        "Four beeps on scan: DataWedge decode error - re-pair sled, check firmware",
        "RFD40 not in DataWedge reader list: update firmware to 006-R01+; Premium Plus required for RFID DataWedge",
        "eConnex not detected: verify correct adapter SKU and 8-pin TC rear connector",
        "Tag not found in app: tag not linked - use Assign RFID Tag in FRISL",
    ]:
        pdf.bullet(item)

    # Ch 24 - API Integration
    pdf.add_page()
    pdf.chapter_title("CHAPTER 24 - RFID API INTEGRATION (PRODUCTION)")
    pdf.body_text(
        "For fixed door readers, batch stock take, or custom Zebra apps, POST scan data "
        "to the FRISL REST API. API documentation is available at /swagger in development."
    )

    pdf.section_title("Single Door / Portal Scan")
    pdf.body_text("POST /api/monitoring/rfid-scan")
    pdf.code_block(
        '{\n'
        '  "rfidCode": "RFID-000101",\n'
        '  "doorLocation": "Main Gate"\n'
        '}'
    )
    pdf.body_text("Used for exit monitoring and door alerts (/Workflow/Rfid).")

    pdf.section_title("Batch Scans (Stock Take / Handheld Upload)")
    pdf.body_text("POST /api/integration/rfid-batch")
    pdf.code_block(
        '{\n'
        '  "sourceSystem": "Zebra-RFD40-TC26",\n'
        '  "scans": [\n'
        '    { "rfidCode": "RFID-000101", "doorLocation": "IT Department" },\n'
        '    { "rfidCode": "RFID-000102", "doorLocation": "IT Department" }\n'
        '  ]\n'
        '}'
    )
    pdf.body_text(
        "A small middleware service or Zebra RFID SDK app on the TC can collect scans "
        "during a stock walk and upload them in one batch."
    )

    pdf.section_title("Fixed Reader Middleware Architecture")
    pdf.code_block(
        "RFID antenna -> Fixed reader (FX9600/Impinj)\n"
        "            -> Middleware service\n"
        "            -> POST /api/monitoring/rfid-scan\n"
        "            -> FRISL exit monitoring & alerts"
    )

    pdf.section_title("Connection Method Summary")
    for item in [
        "Keyboard wedge / DataWedge: Yes - works with web forms today (low effort)",
        "REST API: Yes - for doors and batch uploads (medium effort)",
        "Direct browser SDK: Not built in MVP (high effort)",
        "Native mobile app with Zebra SDK: Optional for production batch scans",
    ]:
        pdf.bullet(item)

    output_path.parent.mkdir(parents=True, exist_ok=True)
    pdf.output(str(output_path))


if __name__ == "__main__":
    out = Path(__file__).resolve().parents[1] / "docs" / "FRISL-RFID-Asset-Management-User-Training-Manual.pdf"
    build_pdf(out)
    print(f"Generated: {out}")
