# PokerTracker2 🃏

A modern, feature-rich poker session tracking application built with WPF (.NET 8) and Firebase integration.

## 🚀 Features

### Core Functionality
- **Session Management**: Create, save, load, and manage poker sessions
- **Player Profiles**: Comprehensive player management with authentication
- **Real-time Tracking**: Live buy-ins, cash-outs, and session statistics
- **Transaction History**: Detailed audit trail for all financial transactions
- **Permission System**: Role-based access control (Admin/Player)
- **Firebase Integration**: Cloud-based data storage and synchronization

### User Experience
- **Modern UI**: Clean, intuitive interface with dark theme
- **Responsive Design**: Adaptive layouts for different screen sizes
- **Activity Logging**: Comprehensive logging with in-app debug console
- **Crash Protection**: Persistent logging to `lastlog.txt` for post-mortem analysis

### Advanced Features
- **Session Activity Panel**: Complete transaction history visualization
- **Player Analytics**: Session statistics and performance tracking
- **Admin Controls**: User management and system administration
- **QuickStart Mode**: Development/testing bypass for rapid iteration

## 🛠️ Technology Stack

- **Frontend**: WPF (.NET 8) with WPF-UI framework
- **Backend**: .NET 8 with Firebase integration
- **Architecture**: MVVM pattern with comprehensive service layer
- **Authentication**: Firebase-based user management with password hashing
- **Data Storage**: Firebase Firestore for cloud synchronization
- **Logging**: Centralized logging service with real-time UI display

## 📋 Prerequisites

- **.NET 8 SDK** or later
- **Visual Studio 2022** (recommended) or **VS Code**
- **Firebase Project** with Firestore database
- **Git** for version control

## 🔧 Setup Instructions

### 1. Clone the Repository
```bash
git clone <your-repository-url>
cd PokerTracker2
```

### 2. Firebase Configuration
1. Create a Firebase project at [Firebase Console](https://console.firebase.google.com/)
2. Enable Firestore database
3. Add your Firebase configuration to the project
4. Set up authentication rules for your use case

### 3. Build and Run
```bash
dotnet restore
dotnet build
dotnet run
```

### 4. QuickStart Development
- Use the "QuickStart" button to bypass authentication during development
- This creates an admin user for testing purposes
- All features are available without Firebase setup

## 🏗️ Project Structure

```
PokerTracker2/
├── Models/                 # Data models and entities
├── Services/              # Business logic and external integrations
├── Windows/               # Main application windows
├── Dialogs/               # Modal dialogs and user interactions
├── ViewModels/            # MVVM view models (if applicable)
├── AIMANIFEST.txt         # Project documentation and progress tracking
└── README.md             # This file
```

### Key Components

#### Models
- **Player**: Session-scoped player data with transaction history
- **PlayerProfile**: Persistent player profile with authentication
- **Session**: Poker session management and metadata
- **User**: User authentication and role management

#### Services
- **SessionManager**: Core session logic and player management
- **PlayerManager**: Player profile CRUD operations
- **FirebaseService**: Cloud data synchronization
- **LoggingService**: Centralized logging and crash protection
- **PermissionService**: Role-based access control

#### Windows
- **MainWindow**: Primary application interface
- **LoginWindow**: User authentication and profile selection
- **PlayerSelectionDialog**: Player addition and management

## 🔐 Authentication & Permissions

### User Roles
- **Admin**: Full system access, user management, session administration
- **Player**: Session participation, profile management (limited)

### Permission System
- **Session Hosting**: Any player can host sessions
- **Session Editing**: Hosts can edit their own sessions, admins can edit all
- **Profile Management**: Admins can manage all profiles, players can create basic profiles
- **Admin Status**: Only existing admins can promote/demote users

## 📊 Session Management

### Creating Sessions
1. Navigate to "New Session" page
2. Click "👥 Add Player" to add participants
3. Set initial buy-ins for each player
4. Save session template or start active session

### Managing Active Sessions
- **Add Players**: Use PlayerSelectionDialog for seamless player addition
- **Track Transactions**: Real-time buy-ins and cash-outs
- **Session Activity**: Complete transaction history with timestamps
- **End Sessions**: Finalize and archive completed sessions

### Player Management
- **Profile Creation**: Comprehensive player profiles with contact information
- **Password Protection**: Secure authentication with salted hashing
- **Session History**: Track participation across multiple sessions
- **Analytics**: Performance statistics and session summaries

## 🚨 Troubleshooting

### Common Issues

#### Build Errors
- Ensure .NET 8 SDK is installed
- Run `dotnet restore` before building
- Check for missing NuGet packages

#### Firebase Connection
- Verify Firebase configuration
- Check internet connectivity
- Review Firebase console for authentication errors

#### UI Issues
- Check debug console for error messages
- Review `lastlog.txt` for crash information
- Verify WPF-UI package installation

### Debug Console
- Use the in-app debug console for real-time logging
- Check `lastlog.txt` in project root for persistent logs
- Enable detailed logging in LoggingService for troubleshooting

## 🔄 Development Workflow

### Git Workflow
1. **Feature Branches**: Create feature branches for new functionality
2. **Regular Commits**: Commit changes frequently with descriptive messages
3. **Pull Requests**: Use pull requests for code review and integration
4. **Version Tags**: Tag releases for version management

### Code Standards
- **C# Conventions**: Follow Microsoft C# coding conventions
- **MVVM Pattern**: Maintain separation of concerns
- **Error Handling**: Comprehensive try-catch blocks with logging
- **Documentation**: Update AIMANIFEST.txt with all changes

## 📈 Future Enhancements

### Planned Features
- **Bulk Operations**: Multi-player management and batch processing
- **Advanced Analytics**: Statistical analysis and trend identification
- **Export Functionality**: Data export to various formats
- **Mobile Support**: Cross-platform compatibility

### Architecture Improvements
- **Unit Testing**: Comprehensive test coverage
- **Performance Optimization**: Memory and CPU usage improvements
- **Plugin System**: Extensible architecture for custom features
- **API Integration**: REST API for external integrations

## 🤝 Contributing

### Development Guidelines
1. **Fork the Repository**: Create your own fork for contributions
2. **Feature Development**: Implement features in dedicated branches
3. **Code Review**: Ensure all code follows project standards
4. **Testing**: Verify functionality before submitting changes
5. **Documentation**: Update relevant documentation and manifests

### Issue Reporting
- Use GitHub Issues for bug reports and feature requests
- Include detailed reproduction steps and error messages
- Attach relevant logs and screenshots when possible

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🙏 Acknowledgments

- **WPF-UI Team**: Modern UI components and styling
- **Firebase Team**: Cloud infrastructure and real-time database
- **Community Contributors**: Feedback, testing, and feature suggestions

## 📞 Support

For support and questions:
- **GitHub Issues**: Report bugs and request features
- **Documentation**: Check AIMANIFEST.txt for project status
- **Community**: Engage with other developers and users

---

**Happy Poker Tracking! 🃏✨** 