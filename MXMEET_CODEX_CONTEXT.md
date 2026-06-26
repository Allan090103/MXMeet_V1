# MXMeet Codex Context

## Project
MXMeet is a Final Year Project Mixed Reality meeting platform built in Unity for Meta Quest. It extends/integrates with the existing VRMeetUp project. The goal is not to rebuild VRMeetUp, networking, or voice chat, but to add Mixed Reality features and MXMeet-specific modules.

## Development Scope
Implement MXMeet only. VRMeetUp is treated as an existing external platform.

MXMeet must provide:
1. User registration and login
2. Avatar selection/customization
3. Main Menu
4. View VRMeetUp in real environment using Meta XR Passthrough and Spatial Anchor
5. Discussion Lobby
6. Create lobby
7. Join lobby
8. Leave lobby
9. End lobby
10. Firestore records for user, avatar, lobby member, discussion lobby, and meetup log

## Out of Scope
Do not rebuild:
- VRMeetUp backend
- Custom networking system
- Custom voice system
- Full desktop/mobile version
- AI meeting assistant

## Technology Stack
- Unity 2022.3 LTS
- C#
- Meta XR SDK
- Firebase Authentication
- Cloud Firestore
- Existing VRMeetUp framework
- XRINetworkGameManager for networking/session
- VoiceChatManager/Vivox for voice

## Architecture
Use layered structure:

Presentation Layer:
- LoginUI
- RegisterUI
- AvatarSelectionUI
- MainMenuUI
- VRMeetUpViewUI
- LobbyUI

Controller Layer:
- AuthController
- AvatarController
- VRMeetUpIntegrationController
- LobbyController_V2

Model / Domain Layer:
- UserModel
- AvatarModel
- DiscussionLobbyModel
- LobbyMemberModel
- MeetupLogModel
- MREnvironmentModel

Data Access Layer:
- FirebaseManager
- Firestore read/write helpers

External Systems:
- Firebase Authentication
- Firebase Firestore
- Meta XR SDK
- VRMeetUp / XRINetworkGameManager
- VoiceChatManager

UI should never call Firebase, Meta XR SDK, or VRMeetUp managers directly. UI calls Controllers. Controllers call FirebaseManager, domain models, Meta XR SDK, or XRINetworkGameManager.

## Main User Flow
1. User launches MXMeet.
2. User registers or logs in using Firebase Authentication.
3. System loads UserModel and AvatarModel from Firestore.
4. User reaches Main Menu.
5. User may:
   - View VRMeetUp in real environment
   - Create Discussion Lobby
   - Join Discussion Lobby
   - Change avatar
   - Logout

## Firebase Collections
Use these Firestore collections:

### users
Fields:
- userId
- username
- email
- createdAt

### avatars
Fields:
- avatarId
- userId
- skinType
- colorCode
- updatedAt

### discussion_lobbies
Fields:
- lobbyId
- hostUserId
- lobbyName
- roomCode
- status: waiting | active | ended
- createdAt
- endedAt

### lobby_members
Fields:
- memberId
- lobbyId
- userId
- username
- role: host | guest
- joinedAt
- leftAt
- status: active | left

### meetup_logs
Fields:
- logId
- lobbyId
- userId
- action: Lobby Created | User Joined | User Left | Lobby Ended
- timestamp
- duration

## Use Cases
UC01 Register User:
- Input username, email, password.
- Validate input.
- Create Firebase Auth account.
- Create users document.
- Create default avatars document.

UC02 Authenticate User:
- Login using email and password.
- Load users/{uid}.
- Load avatars/{uid}.
- Store CurrentUser and CurrentAvatar.

UC03 Choose Avatar:
- Display available avatar options.
- Save selected skinType and colorCode to Firestore.
- Update CurrentAvatar.

UC04 View VRMeetUp in Real Environment:
- Enable Meta XR Passthrough.
- Create/place Spatial Anchor.
- Place VRMeetUp screen/panel at anchor.
- Connect to VRMeetUp using XRINetworkGameManager quick join or room code.
- On exit, disconnect and disable passthrough if needed.

UC05 Create Discussion Lobby:
- Authenticated user becomes host.
- Call XRINetworkGameManager.CreateNewLobby or existing VRMeetUp lobby creation method.
- Get room code.
- Save discussion_lobbies document.
- Add host to lobby_members.
- Add meetup_logs entry.
- Display room code and active participant list.

UC06 Join Discussion Lobby:
- Guest enters room code.
- Validate lobby exists and status is active/waiting.
- Call XRINetworkGameManager.JoinLobbyByCode or existing join method.
- Save lobby_members entry.
- Add meetup_logs entry.
- Spawn/show guest avatar in host environment.

UC07 Leave Discussion Lobby:
- Guest leaves active lobby.
- Update lobby_members leftAt/status.
- Add meetup_logs entry.
- Remove avatar from lobby scene.
- Disconnect through XRINetworkGameManager if appropriate.

UC08 End Discussion Lobby:
- Host ends lobby.
- Update discussion_lobbies status to ended.
- Update active lobby_members.
- Add meetup_logs entry.
- Disconnect participants.
- Remove spawned avatars.

## Important Implementation Rules
- Keep code simple and FYP-level achievable.
- Prefer small focused scripts.
- Do not modify VRMeetUp core scripts unless absolutely required.
- If integration is needed, create adapter/wrapper scripts.
- Use async/await for Firebase where possible.
- Add null checks and readable error messages.
- Keep class names consistent with SRS/SDD.
- Avoid overengineering.
- UI should be basic and functional first.

## Suggested Implementation Order
1. FirebaseManager setup
2. UserModel and AvatarModel
3. Register/Login UI and AuthController
4. Main Menu navigation
5. Avatar selection and save/load
6. Passthrough enable/disable
7. Spatial anchor placement
8. VRMeetUp view connection
9. Create Discussion Lobby
10. Join Discussion Lobby
11. Leave/End Lobby
12. Firestore logging
13. Testing according to STD

## Expected UI Screens
Main Menu:
- Launch VRMeetUp
- Notepad optional
- Create Discussion Lobby
- Settings
- Logout

VRMeetUp MR View:
- Passthrough ON
- Anchor Placed
- VRMeetUp screen/panel
- Reposition
- Exit VRMeetUp

Discussion Lobby:
- Top bar: Discussion Lobby, active guests, room code
- Avatar markers/names in real environment
- Active participant panel
- Mute button
- Leave Lobby or End Lobby button

## Testing Focus
Test:
- Register valid user
- Duplicate username
- Invalid email/password
- Login valid/invalid
- Avatar save/load
- Passthrough enable
- Anchor placement
- VRMeetUp quick join
- Lobby creation
- Join lobby with valid/invalid code
- Guest leave
- Host end lobby
- Firebase unavailable/network failure

## Examiner-Safe Explanation
MXMeet contributes the Mixed Reality layer, Firebase-based MXMeet data management, avatar/lobby flow, and Discussion Lobby interaction. VRMeetUp is reused only for existing session, networking, avatar synchronization, and voice communication.