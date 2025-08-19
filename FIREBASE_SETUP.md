# Firebase Setup Guide for PokerTracker2

This guide will walk you through setting up Firebase for the PokerTracker2 application for **data storage only**. Authentication is handled locally with player profile names and passwords.

## Prerequisites

- Google account
- Basic understanding of Firebase services
- .NET 8.0 development environment

## Step 1: Create a Firebase Project

1. **Go to Firebase Console**
   - Visit [https://console.firebase.google.com/](https://console.firebase.google.com/)
   - Sign in with your Google account

2. **Create New Project**
   - Click "Create a project"
   - Enter project name: `pokertracker2-xxxxx` (replace xxxxx with your unique identifier)
   - Click "Continue"

3. **Configure Google Analytics (Optional)**
   - Choose whether to enable Google Analytics
   - Click "Continue"

4. **Project Created**
   - Click "Continue" to proceed to the project dashboard

## Step 2: Enable Firestore Database

1. **Navigate to Firestore**
   - In the left sidebar, click "Firestore Database"
   - Click "Create database"

2. **Choose Security Rules**
   - Select "Start in test mode" for development
   - Click "Next"

3. **Choose Location**
   - Select a location close to your users (e.g., `us-central1` for US)
   - Click "Done"

4. **Database Created**
   - Your Firestore database is now ready

## Step 3: Create Service Account

1. **Navigate to Project Settings**
   - Click the gear icon next to "Project Overview"
   - Select "Project settings"

2. **Service Accounts Tab**
   - Click on "Service accounts" tab
   - Click "Generate new private key"

3. **Download Key File**
   - Click "Generate key"
   - Download the JSON file
   - **Important**: Keep this file secure and never commit it to version control

4. **Rename and Move File**
   - Rename the file to `firebase-credentials.json`
   - Place it in one of these locations:
     - `%APPDATA%\PokerTracker2\firebase-credentials.json` (recommended)
     - `[Project Root]\firebase-credentials.json`
     - `[Project Root]\..\firebase-credentials.json`

## Step 4: Configure Security Rules

1. **Navigate to Firestore Rules**
   - Go to Firestore Database → Rules

2. **Update Rules**
   - Replace the default rules with:

```javascript
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    // Allow read/write access to all documents (for development)
    // In production, you can restrict this based on your needs
    match /{document=**} {
      allow read, write: if true;
    }
  }
}
```

3. **Publish Rules**
   - Click "Publish"

## Step 5: Update Application Configuration

1. **Update Project ID**
   - Open `PokerTracker2/Services/FirebaseService.cs`
   - Update the `_projectId` parameter in the constructor:
   ```csharp
   public FirebaseService(string projectId = "your-actual-project-id", string credentialsPath = "")
   ```

2. **Environment Variable (Alternative)**
   - Set the `GOOGLE_APPLICATION_CREDENTIALS` environment variable:
   ```powershell
   $env:GOOGLE_APPLICATION_CREDENTIALS = "C:\Users\[YourUsername]\AppData\Roaming\PokerTracker2\firebase-credentials.json"
   ```

## Step 6: Test the Setup

1. **Build the Application**
   ```powershell
   dotnet build
   ```

2. **Run the Application**
   ```powershell
   dotnet run
   ```

3. **Test Firebase Connection**
   - The application will automatically test the Firebase connection
   - Check the console output for connection status

## How Authentication Works

**Local Authentication (What You Have):**
- Player profiles can have optional passwords
- Passwords are hashed with SHA256 + salt locally
- Login uses profile name + password combination
- No email required - just profile name and password

**Firebase (Data Storage Only):**
- Stores player profiles, sessions, and other data
- Syncs data across devices
- Provides backup and cloud storage
- Does NOT handle authentication

## Data Structure

The application uses the following Firestore collections:

### player_profiles
```json
{
  "name": "string",
  "nickname": "string",
  "email": "string",
  "phone": "string",
  "notes": "string",
  "hasPassword": "boolean",
  "passwordHash": "string",
  "salt": "string",
  "createdDate": "timestamp",
  "lastPlayedDate": "timestamp",
  "totalSessionsPlayed": "number",
  "totalLifetimeBuyIn": "number",
  "totalLifetimeCashOut": "number",
  "isActive": "boolean"
}
```

### sessions
```json
{
  "id": "string",
  "name": "string",
  "startTime": "timestamp",
  "endTime": "timestamp",
  "notes": "string",
  "totalBuyIns": "number",
  "totalCashOuts": "number",
  "netProfit": "number",
  "createdAt": "timestamp",
  "updatedAt": "timestamp"
}
```

## Troubleshooting

### Common Issues

1. **Authentication Failed**
   - Verify the service account key file path
   - Check that the project ID is correct
   - Ensure the service account has proper permissions

2. **Permission Denied**
   - Check Firestore security rules
   - Verify the service account has the necessary roles
   - Ensure the database is in the correct region

3. **Connection Timeout**
   - Check your internet connection
   - Verify firewall settings
   - Check if the Firebase project is active

### Debug Information

The application logs Firebase operations to the console. Look for:
- `Firebase initialization failed: [error message]`
- `Failed to save player profile to Firebase: [error message]`
- `Firebase connection test failed: [error message]`

## Security Considerations

1. **Service Account Key**
   - Never commit the service account key to version control
   - Use environment variables or secure file storage
   - Rotate keys regularly

2. **Security Rules**
   - Start with permissive rules for development
   - Implement proper restrictions for production
   - Use authentication to control access when needed

3. **Data Validation**
   - Always validate input data
   - Use proper data types and constraints
   - Implement rate limiting for production use

## Production Deployment

1. **Update Security Rules**
   - Implement proper access controls
   - Add rate limiting rules
   - Set up proper indexes

2. **Monitoring**
   - Enable Firebase Analytics
   - Set up error reporting
   - Monitor database usage and costs

3. **Backup Strategy**
   - Set up automated backups
   - Implement disaster recovery procedures
   - Test restore procedures regularly

## Support

If you encounter issues:

1. Check the Firebase Console for error messages
2. Review the application console output
3. Check Firebase documentation: [https://firebase.google.com/docs](https://firebase.google.com/docs)
4. Review the application logs in `%APPDATA%\PokerTracker2\PokerTracker2.log`

## Next Steps

Once Firebase is set up:

1. **Test Player Profile Creation**
   - Create a new player profile with a password
   - Verify it appears in the login dropdown

2. **Test Authentication**
   - Try logging in with the created profile
   - Verify the password verification works

3. **Test Data Persistence**
   - Create sessions and verify they're saved to Firebase
   - Check that data persists between application restarts

4. **Monitor Usage**
   - Check Firebase Console for data
   - Monitor data operations
   - Review security logs
