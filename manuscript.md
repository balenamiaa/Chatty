# 1. System Architecture Overview

## 1.1 Core Architecture

- **Monolithic Server Architecture**: The system is designed as a monolithic application using a solution with multiple projects to streamline development, testing, and deployment processes.

- **Solution Structure**: The solution contains three primary projects:

  - **Backend**: The server-side application handling API requests, WebSocket connections, and business logic, built with **ASP.NET Core**.

  - **Shared**: Contains shared code such as models, utilities, and constants used across the project, implemented as a **.NET Standard** or **.NET 5/6/7** class library.

  - **Client**: A shared client library used by the frontend and possibly other clients, ensuring consistent communication protocols and data models.

- **End-to-End Type Safety**: Uses **Carter** or **Endpoint Routing** with **FluentValidation** for route definitions, providing end-to-end type safety similar to **Tapir** in Scala. This ensures that the routes and data contracts are strongly typed and validated across client and server.

- **Event-Driven Architecture**: Utilizes **Reactive Extensions (Rx.NET)** and **async/await** for asynchronous programming, ensuring efficient event propagation and handling in real-time communication.

- **End-to-End Encryption**: All messages and files are encrypted end-to-end **on the client side** using **.NET cryptography libraries**, maintaining user privacy and data security. The server stores only encrypted data and cannot read user content.

- **Zero-Trust Security Model**: The system assumes no implicit trust between components, enforcing strict authentication and authorization at every layer using industry-standard security practices.

- **Local Encrypted File Storage**: Files are stored locally on the server with robust encryption to prevent unauthorized access and to ensure data integrity, utilizing **.NET's security features**.

## 1.2 Key Components

- **Backend Server**: Built with **ASP.NET Core Web API**, using **Carter** for minimal APIs and route safety, providing core API endpoints and business logic.

- **WebSocket Server**: Uses **SignalR**, which provides a simple and reliable way to handle real-time bidirectional communication with clients.

- **Database**: **PostgreSQL** is used alongside **Entity Framework Core (EF Core)**, an object-relational mapper (ORM) for .NET, for robust and efficient data management. Advanced features like partitioning and full-text search are supported.

- **File Storage System**: Manages encrypted file storage, handling uploads, downloads, encryption, decryption, and file integrity checks. All encryption and decryption occur **on the client side**.

- **Push Notification Service**: Sends real-time notifications to users across different platforms, integrating with **APNs** for iOS, **FCM** for Android, and **Web Push Protocol** for web clients.

- **Frontend SPA**: Developed using **Blazor WebAssembly**, providing a reactive and responsive user interface that runs in the browser as a single-page application.

- **Shared Client Library**: Contains shared logic, models, and utilities between the frontend and backend, ensuring consistency and reducing code duplication. Uses **OneOf** or **LanguageExt** for Algebraic Data Types (ADTs) to model data more expressively.

- **OpenAPI Specification**: Uses **NSwag** or **Swashbuckle** to define API endpoints and generate OpenAPI documentation, ensuring the client and server stay in sync with strong typing.

---

# 2. Feature Set

The feature set remains consistent with the original design, providing comprehensive messaging, server and channel management, user customization, voice and video capabilities, and mobile support.

---

# 3. Technical Architecture

## 3.1 Backend Architecture

### 3.1.1 Core Structure

- **Functional Approach**: The backend leverages modern C# features to adopt a more functional programming style, using **records**, **pattern matching**, **immutable data structures**, and **LINQ**.

- **Layered Architecture**: The backend is structured into layers to separate concerns:

  - **Transport Layer**: Handles network communication via HTTP and WebSockets using **Carter** for minimal APIs.

  - **Service Layer**: Contains business logic and operations.

  - **Repository Layer**: Manages data persistence and retrieval from the database.

### 3.1.2 Transport Layer

```
Backend/
  Modules/
    Api/          // Carter modules for HTTP endpoints
    SignalR/      // SignalR hubs for real-time communication
  Middleware/     // Authentication, logging, rate limiting
  Validators/     // Request validation logic using FluentValidation
  OpenApi/        // OpenAPI documentation configuration
```

- **Carter Modules**: Define HTTP endpoints with strong typing and minimal boilerplate, similar to **Tapir**, ensuring end-to-end route safety.

- **SignalR Hubs**: Manage real-time communication for messaging, typing indicators, presence updates, and voice/video signaling.

- **Middleware**: Implement cross-cutting concerns like authentication, authorization, logging, and rate limiting, leveraging **ASP.NET Core's middleware pipeline**.

- **Validators**: Validate incoming requests using **FluentValidation**, ensuring data integrity and security.

- **OpenAPI Generation**: Automatically generate API documentation using **NSwag** or **Swashbuckle**, integrating with Carter modules.

### 3.1.3 Service Layer

```
Backend/
  Services/
    Auth/         // Authentication & authorization services
    Message/      // Message processing
    Channel/      // Channel management services
    Server/       // Server (community) management
    User/         // User account management
    File/         // File upload/download handling
    Notification/ // Push notification services
    Presence/     // User presence tracking
    Search/       // Message and content search functionality
    Voice/        // Voice and video communication handling
```

- **Functional Services**: Services use immutable data structures and functional patterns, avoiding side effects where possible.

- **Authentication & Authorization**: Handle user authentication, session management, and permission checks using **JWT tokens** and policies.

- **Message Service**: Manage message metadata (since content is encrypted on the client), message indexing, and delivery.

- **Channel Service**: Create, update, and delete channels; manage channel settings and permissions.

- **Server Service**: Manage servers (communities), including creation, deletion, and member management.

- **User Service**: Handle user profile updates, settings, and relationships.

- **File Service**: Manage file uploads, downloads, and storage, ensuring that files are encrypted on the client side before upload.

- **Notification Service**: Send push notifications and manage notification settings.

- **Presence Service**: Track user online/offline status and activity.

- **Search Service**: Provide search functionality across messages and content, using encrypted indexes if necessary.

- **Voice Service**: Handle voice and video call setup, signaling, and state management.

### 3.1.4 Repository Layer

```
Backend/
  Data/
    Models/       // EF Core models
    Context/      // Database context definitions
    Migrations/   // Database migration scripts
    Repositories/ // Data access implementations
```

- **Functional Data Access**: Repositories return immutable data structures and use async methods extensively.

- **Models**: Define database models using **records** for immutability.

- **Context**: Maintain the database context for EF Core, configured for PostgreSQL.

- **Migrations**: Use **EF Core migrations** for database schema migrations.

- **Repositories**: Implement data access logic, using LINQ and functional patterns.

### 3.1.5 Core Systems

- **Real-time Communication System**:

  - Utilizes **SignalR** for real-time communication, benefiting from its strong typing and built-in scalability.

  - Manages active connections and tracks user presence, using **ConcurrentDictionary** and other thread-safe collections.

  - Enforces rate limiting using middleware and possibly **Polly** for resilience.

- **File Storage System**:

  - Stores files encrypted on the client, ensuring the server handles only binary data without access to plaintext.

  - Supports streaming uploads and downloads to handle large files efficiently.

  - Performs integrity checks using hashes provided by the client.

- **Push Notification System**:

  - Manages device tokens and user preferences for notifications.

  - Supports notification grouping and silent notifications.

  - Tracks delivery status to ensure reliability and allow retries.

## 3.2 Frontend Architecture

### 3.2.1 Core Structure

```
Frontend/
  Components/     // Reusable UI components
  Pages/          // Route-specific pages
  Stores/         // State management using Fluxor or a functional library
  Services/       // HTTP clients, SignalR clients
  Validators/     // Client-side validation logic
  Styles/         // CSS and styling resources
  Utilities/      // Utility functions and helpers
  wwwroot/        // Static assets
```

- **Components**: Built using **Blazor's** component model, utilizing **functional components** where possible.

- **Pages**: Define the views for each route in the application, using **.razor** files.

- **Stores**: Manage application state using **Fluxor** or **Fluxor.Blazor**, adopting a unidirectional data flow.

- **Services**: Include HTTP clients generated from OpenAPI specs, SignalR clients for real-time communication, and other client-side services.

- **Validators**: Use **FluentValidation** for client-side validation, ensuring consistency with server-side validation.

- **Styles**: Organize CSS, possibly using **CSS isolation** in Blazor, and adopt a design system.

- **Utilities**: Provide utility functions for common tasks, possibly using functional programming paradigms.

### 3.2.2 Component Architecture

- **Atomic Design Pattern**:

  - **Atoms**: Basic UI elements (buttons, inputs, labels), implemented as functional components.

  - **Molecules**: Combinations of atoms forming functional units (form fields, navigation items).

  - **Organisms**: Complex components composed of molecules and atoms (headers, footers, message lists).

  - **Templates**: Page layouts defining the structure without specific content.

  - **Pages**: Complete views with content, forming the different routes in the application.

### 3.2.3 State Management

- Uses **Fluxor** for state management, embracing immutability and functional programming concepts.

- **Global State** includes:

  - User session data.

  - Current server and channel.

  - Unread message counts.

  - Presence information.

  - Active voice/video calls.

  - Draft messages.

  - Settings and preferences.

### 3.2.4 Progressive Web App Features

- **Offline Support**: Uses service workers to cache assets and data, leveraging Blazor's PWA capabilities.

- **Push Notifications**: Integrates with the push notification service using **JavaScript interop**.

- **Background Sync**: Syncs data when connectivity is restored.

- **App Shell Architecture**: Provides a consistent and fast-loading UI shell.

- **Cache Management**: Ensures the app remains up-to-date without unnecessary data usage.

## 3.3 Client Library Architecture

### 3.3.1 Core Structure

```
ClientLibrary/
  Api/            // Generated API client from OpenAPI spec
  Crypto/         // Encryption and decryption utilities
  Storage/        // Local storage management
  State/          // Client-side state management
  Events/         // Event handling system
  Sync/           // Offline synchronization mechanisms
  Models/         // Shared data models using OneOf or LanguageExt
  Utilities/      // Helper functions
```

- **API**: Provides a client for making API calls to the backend, generated from OpenAPI specs using **NSwag**, ensuring strong typing.

- **Crypto**: Implements cryptographic functions for end-to-end encryption using **.NET's cryptography libraries**. All encryption happens **on the client side**.

- **Storage**: Manages local storage, including caching and persistence, using **Blazor's local storage** or **IndexedDB**.

- **State**: Handles client-side state, using immutable data structures and functional patterns.

- **Events**: Manages real-time events from the server via **SignalR** connections.

- **Sync**: Implements mechanisms for syncing data when transitioning between offline and online states.

- **Models**: Uses **OneOf** or **LanguageExt** for ADTs, enabling expressive and type-safe modeling of data.

### 3.3.2 Features

- **API Client Generation**: Automatically generates clients from OpenAPI specs using **NSwag**, ensuring type safety.

- **Automatic Retries**: Implements retry logic with exponential backoff, possibly using **Polly**.

- **Request Queuing**: Queues API requests when offline, ensuring data consistency.

- **Offline Support**: Enables the application to function offline with cached data.

- **End-to-End Encryption**: Encrypts and decrypts messages and files **on the client side**.

## 3.4 Shared Architecture

### 3.4.1 Core Structure

```
Shared/
  Models/         // Shared data models and types using records and ADTs
  Validation/     // Shared validation rules and schemas with FluentValidation
  Constants/      // Application-wide constants
  Errors/         // Shared error types using OneOf or LanguageExt
  Utilities/      // Utility functions and helpers
  Crypto/         // Shared cryptographic functions
```

- **Models**: Define data models and types used across the application, using **records** and **ADTs** for expressiveness.

- **Validation**: Contains validation schemas and rules, using **FluentValidation**, ensuring consistency between client and server.

- **Constants**: Holds constants like permission flags and error codes.

- **Errors**: Defines common error types and handling mechanisms, using **OneOf** or **LanguageExt** to model errors as ADTs.

- **Utilities**: Provides utility functions for common tasks, possibly using functional programming paradigms.

- **Crypto**: Shared cryptographic functions, ensuring consistent encryption protocols between client and server.

---

# 4. Implementation Details

## 4.1 Database Schema

**Note**: The database schema uses PostgreSQL with the `pgcrypto` extension for UUID generation and cryptographic functions. **Entity Framework Core** is used for database interactions with code-first migrations. Below are the SQL statements for setting up the database.

### 4.1.1 SQL Statements

```sql
-- Enable pgcrypto extension for UUID generation
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Users Table
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    username VARCHAR(50) NOT NULL UNIQUE,
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    first_name VARCHAR(50),
    last_name VARCHAR(50),
    profile_picture_url TEXT,
    status_message TEXT,
    last_online_at TIMESTAMPTZ,
    locale VARCHAR(10) DEFAULT 'en-US',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_users_username ON users(username);
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_last_online ON users(last_online_at);

-- User Devices Table (for E2E encryption)
CREATE TABLE user_devices (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    device_id UUID NOT NULL,
    device_name VARCHAR(100),
    public_key BYTEA NOT NULL,
    device_token TEXT,
    device_type VARCHAR(20) NOT NULL CHECK (device_type IN ('ios', 'android', 'web', 'desktop')),
    last_active_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (user_id, device_id)
);

CREATE INDEX idx_user_devices_user ON user_devices(user_id);
CREATE INDEX idx_user_devices_token ON user_devices(device_token);

-- Pre-Keys Table (for E2E encryption)
CREATE TABLE pre_keys (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    device_id UUID NOT NULL,
    pre_key_id INTEGER NOT NULL,
    pre_key_public BYTEA NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (user_id, device_id, pre_key_id)
);

CREATE INDEX idx_pre_keys_user_device ON pre_keys(user_id, device_id);

-- Servers (Communities) Table
CREATE TABLE servers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    owner_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    icon_url TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_servers_owner ON servers(owner_id);

-- Server Roles Table
CREATE TABLE server_roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    server_id UUID NOT NULL REFERENCES servers(id) ON DELETE CASCADE,
    name VARCHAR(50) NOT NULL,
    color VARCHAR(7),
    is_default BOOLEAN NOT NULL DEFAULT FALSE,
    permissions JSONB NOT NULL DEFAULT '{}',
    position INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (server_id, name)
);

CREATE INDEX idx_server_roles_server ON server_roles(server_id);

-- Server Members Table
CREATE TABLE server_members (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    server_id UUID NOT NULL REFERENCES servers(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    role_id UUID REFERENCES server_roles(id) ON DELETE SET NULL,
    nickname VARCHAR(50),
    joined_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (server_id, user_id)
);

CREATE INDEX idx_server_members_server ON server_members(server_id);
CREATE INDEX idx_server_members_user ON server_members(user_id);

-- Channels Table
CREATE TABLE channels (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    server_id UUID REFERENCES servers(id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL,
    topic TEXT,
    is_private BOOLEAN NOT NULL DEFAULT FALSE,
    channel_type VARCHAR(20) NOT NULL DEFAULT 'text' CHECK (channel_type IN ('text', 'voice', 'video')),
    position INTEGER NOT NULL DEFAULT 0,
    rate_limit_per_user INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_channels_server ON channels(server_id);

-- Channel Members Table (for private channels)
CREATE TABLE channel_members (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    channel_id UUID NOT NULL REFERENCES channels(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    joined_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (channel_id, user_id)
);

CREATE INDEX idx_channel_members_channel ON channel_members(channel_id);
CREATE INDEX idx_channel_members_user ON channel_members(user_id);

-- Messages Table (with E2E encryption support)
CREATE TABLE messages (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    channel_id UUID NOT NULL REFERENCES channels(id) ON DELETE CASCADE,
    sender_id UUID NOT NULL REFERENCES users(id) ON DELETE SET NULL,
    content BYTEA NOT NULL,
    content_type VARCHAR(20) NOT NULL DEFAULT 'text',
    sent_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    message_nonce BYTEA NOT NULL,
    key_version INTEGER NOT NULL DEFAULT 1,
    parent_message_id UUID REFERENCES messages(id) ON DELETE CASCADE,
    reply_count INTEGER NOT NULL DEFAULT 0
);

CREATE INDEX idx_messages_channel_sent ON messages(channel_id, sent_at DESC);
CREATE INDEX idx_messages_sender ON messages(sender_id);
CREATE INDEX idx_messages_parent ON messages(parent_message_id) WHERE parent_message_id IS NOT NULL;

-- Direct Messages Table (with E2E encryption)
CREATE TABLE direct_messages (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    sender_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    recipient_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    content BYTEA NOT NULL,
    content_type VARCHAR(20) NOT NULL DEFAULT 'text',
    sent_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    message_nonce BYTEA NOT NULL,
    key_version INTEGER NOT NULL DEFAULT 1,
    parent_message_id UUID REFERENCES direct_messages(id) ON DELETE CASCADE,
    reply_count INTEGER NOT NULL DEFAULT 0
);

CREATE INDEX idx_direct_messages_participants ON direct_messages(sender_id, recipient_id, sent_at DESC);
CREATE INDEX idx_direct_messages_parent ON direct_messages(parent_message_id) WHERE parent_message_id IS NOT NULL;

-- Attachments Table (with E2E encryption)
CREATE TABLE attachments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    message_id UUID REFERENCES messages(id) ON DELETE CASCADE,
    direct_message_id UUID REFERENCES direct_messages(id) ON DELETE CASCADE,
    file_name TEXT NOT NULL,
    file_size BIGINT NOT NULL,
    content_type VARCHAR(255) NOT NULL,
    storage_path TEXT NOT NULL,
    thumbnail_path TEXT,
    encryption_key BYTEA NOT NULL,
    encryption_iv BYTEA NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CHECK (
        (message_id IS NULL AND direct_message_id IS NOT NULL) OR
        (message_id IS NOT NULL AND direct_message_id IS NULL)
    )
);

CREATE INDEX idx_attachments_message ON attachments(message_id);
CREATE INDEX idx_attachments_direct_message ON attachments(direct_message_id);

-- Contacts Table
CREATE TABLE contacts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    contact_user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    status VARCHAR(20) NOT NULL CHECK (status IN ('pending', 'accepted', 'blocked')),
    added_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (user_id, contact_user_id)
);

CREATE INDEX idx_contacts_user_status ON contacts(user_id, status);

-- Voice/Video Calls Table
CREATE TABLE calls (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    channel_id UUID REFERENCES channels(id) ON DELETE SET NULL,
    initiator_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    call_type VARCHAR(10) NOT NULL CHECK (call_type IN ('voice', 'video')),
    started_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    ended_at TIMESTAMPTZ,
    status VARCHAR(20) NOT NULL DEFAULT 'initiated' 
        CHECK (status IN ('initiated', 'ringing', 'connected', 'ended'))
);

CREATE INDEX idx_calls_channel ON calls(channel_id);
CREATE INDEX idx_calls_initiator ON calls(initiator_id);
CREATE INDEX idx_active_calls ON calls(status) WHERE status != 'ended';

-- Call Participants Table
CREATE TABLE call_participants (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    call_id UUID NOT NULL REFERENCES calls(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    joined_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    left_at TIMESTAMPTZ,
    muted BOOLEAN NOT NULL DEFAULT FALSE,
    video_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    UNIQUE (call_id, user_id)
);

CREATE INDEX idx_call_participants_call ON call_participants(call_id);
CREATE INDEX idx_call_participants_user ON call_participants(user_id);

-- Presence Subscriptions Table
CREATE TABLE presence_subscriptions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    subscriber_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    target_user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (subscriber_id, target_user_id)
);

CREATE INDEX idx_presence_subscriptions_subscriber ON presence_subscriptions(subscriber_id);
CREATE INDEX idx_presence_subscriptions_target ON presence_subscriptions(target_user_id);

-- Function to update updated_at timestamps
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE 'plpgsql';

-- Create triggers for updated_at columns
CREATE TRIGGER update_users_updated_at
    BEFORE UPDATE ON users
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_servers_updated_at
    BEFORE UPDATE ON servers
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_channels_updated_at
    BEFORE UPDATE ON channels
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_messages_updated_at
    BEFORE UPDATE ON messages
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();
```

### 4.1.2 Notes on Database Schema

- **End-to-End Encryption Support**: The schema includes tables like `user_devices`, `pre_keys`, and fields like `content BYTEA` and `message_nonce` to support end-to-end encryption. Encryption and decryption occur **on the client side**.

- **Message Structure**: Separate tables for `messages` and `direct_messages` distinguish between channel messages and direct messages.

- **Attachments**: The `attachments` table supports both messages and direct messages, with encrypted storage details.

- **Contacts Management**: The `contacts` table allows users to manage friends, pending requests, and blocked users.

- **Voice/Video Calls**: Tables `calls` and `call_participants` manage call sessions and participants.

- **Triggers and Functions**: Automatically update `updated_at` timestamps on record changes.

- **Entity Framework Core**: Used for database interaction.

- **EF Core Migrations**: Used for database schema migrations.

## 4.2 API Design

### 4.2.1 HTTP API Structure

Defined using **Carter** modules:

- **Endpoints**: Organized under `/api/v1/`.

- **Strongly-Typed Routes**: Using **Carter** and **FluentValidation** for end-to-end route safety and validation, similar to **Tapir**.

- **Authentication**: Endpoints for registration, login, logout, etc.

- **Users**: Endpoints for managing user information and settings.

- **Servers and Channels**: Endpoints for managing servers (communities) and channels.

- **Files**: Endpoints for file upload and download, with files encrypted on the client side.

- **Notifications**: Endpoints for managing notifications.

- **OpenAPI/Swagger Documentation**: Configured using **NSwag** or **Swashbuckle**, integrating with Carter modules for accurate documentation.

### 4.2.2 SignalR Protocol

- **Server Events**: Include message updates, presence updates, notifications, etc., sent from the server to clients.

- **Client Events**: Include message sending, typing indicators, presence updates, etc., sent from clients to the server.

- **Strongly-Typed Hubs**: SignalR hubs are strongly typed, ensuring type safety in real-time communication.

## 4.3 Error Handling

### 4.3.1 Error Types

Defined using **OneOf** or **LanguageExt** to represent various error scenarios as discriminated unions (ADTs), enabling expressive and type-safe error handling.

Example:

```csharp
public abstract record AuthError;

public record InvalidCredentials : AuthError;

public record UserNotFound : AuthError;

public record AccountLocked : AuthError;
```

### 4.3.2 Error Handling Strategy

- **Functional Error Handling**: Use pattern matching and functional techniques to handle errors.

- **Custom Middleware**: Centralized error handling middleware formats error responses consistently.

- **Structured Logging**: Using **Serilog** with enrichers for better analysis and monitoring.

- **Error Recovery Mechanisms**: Implement retries and fallbacks for transient errors using **Polly**.

- **Graceful Degradation**: Ensures basic functionality during partial failures.

- **User-Friendly Messages**: Provide clear and helpful error messages to the client.

## 4.4 State Management

### 4.4.1 Backend State

- **Connection State Tracking**: Use **ConcurrentDictionary** and immutable data structures.

- **User Presence**: Tracked in-memory with regular persistence to the database.

- **Rate Limit Tracking**: Implemented using in-memory caches or distributed caches like **Redis**.

- **Session Management**: Securely managing user sessions with **JWT tokens** and refresh tokens.

### 4.4.2 Frontend State

- **Immutable State**: Using **Fluxor** to manage state immutably, adopting functional programming patterns.

- **State Synchronization**: Via **SignalR** events, ensuring real-time updates.

- **Optimistic Updates**: UI updates immediately, with rollback if server rejects changes.

- **Persistence**: Using **Blazor's** local storage or **IndexedDB** for caching and offline support.

---

# 5. Security Implementation

## 5.1 End-to-End Encryption System

### 5.1.1 Key Management

- **KeyBundle**: Managed **on the client side**, containing identity key, signed pre-key, and one-time pre-keys.

- **SessionKeys**: Used for encrypting messages within a session, ensuring forward secrecy.

- **Libraries**: Utilize **Elliptic Curve Cryptography (ECC)** and **AES-GCM** for encryption, using **.NET's cryptography libraries**.

### 5.1.2 Message Encryption Flow

- **Initial Key Exchange**: Using the **Double Ratchet algorithm**, possibly with the **OliveHelps** library or custom implementation.

- **Message Encryption Pipeline**: Encrypts content and rotates keys for forward secrecy, all **on the client side**.

- **Group Messaging Handling**: Uses **Sender Keys** to efficiently encrypt messages for groups.

## 5.2 File Handling System

### 5.2.1 File Processing Pipeline

- **Client-Side Encryption**: Files are encrypted **on the client** before upload, using **AES-256-GCM**.

- **Validation**: Ensures allowed file types and sizes before encryption.

- **Storage**: Encrypted files stored securely on the server.

- **Integrity Verification**: Using client-provided hashes to verify file integrity.

### 5.2.2 File Storage Structure

- **Hierarchical Organization**: Files stored under directories named by user ID and file ID.

- **Metadata Storage**: Metadata stored in the database, including encryption parameters.

## 5.3 Real-time Communication System

### 5.3.1 SignalR Hubs

- **Strongly-Typed Hubs**: Interfaces define the methods and data contracts, ensuring compile-time safety.

- **Connection Management**: Tracks active connections using thread-safe collections.

- **Authentication**: Enforces authentication and authorization policies on hubs.

- **Message Handling**: Routes messages to appropriate clients or groups.

### 5.3.2 Event Broadcasting

- **Efficient Broadcasting**: Uses **SignalR groups** to send messages only to relevant clients.

- **Subscription Management**: Manages client subscriptions to channels and events dynamically.

## 5.4 Push Notification System

### 5.4.1 Core Structure

- **Platform Support**: Integrates with **APNs**, **FCM**, and **Web Push**.

- **Encryption**: Uses **VAPID** for Web Push encryption.

- **Retry Logic**: Implements exponential backoff for retries.

- **Delivery Tracking**: Monitors delivery status and handles undelivered notifications.

---

# 6. Advanced Systems Implementation

## 6.1 Voice/Video Implementation

### 6.1.1 WebRTC Architecture

- **WebRTC Integration**: Uses **WebRTC** for peer-to-peer audio and video communication.

- **Signaling**: Handled via **SignalR**, providing a reliable signaling channel.

- **Session Management**: Manages active sessions per channel, with state stored in-memory or in a distributed cache.

### 6.1.2 Voice State Management

- **Real-time Updates**: Broadcasts voice state changes to all participants.

- **Mute/Deaf Controls**: Implemented on both client and server sides, ensuring consistent state.

## 6.2 Rate Limiting System

### 6.2.1 Token Bucket Implementation

- **Per-User Limits**: Configurable rate limits per action and per user.

- **Thread Safety**: Uses **ConcurrentDictionary** and **SemaphoreSlim**.

- **Distributed Rate Limiting**: Optionally uses **Redis** for distributed rate limiting.

## 6.3 Permission System

### 6.3.1 Permission Calculation

- **Functional Approach**: Calculates permissions using pure functions and immutable data.

- **Role Hierarchy**: Evaluates user roles and permission overrides.

- **Caching**: Caches permission calculations for performance.

## 6.4 Client State Management

### 6.4.1 State Synchronization

- **Event-Driven Updates**: Uses **SignalR** to receive updates.

- **Conflict Resolution**: Applies **CRDTs** (Conflict-Free Replicated Data Types) or custom merge strategies.

## 6.5 Error Recovery Systems

### 6.5.1 Circuit Breaker

- **Polly Integration**: Uses **Polly** for implementing circuit breakers and retries.

- **Monitoring and Alerts**: Integrated with logging to monitor failures.

---

This architectural rewrite adapts the system to the **C#** and **.NET** ecosystem, leveraging technologies like **ASP.NET Core**, **Entity Framework Core**, **Blazor WebAssembly**, **Carter**, and **SignalR**. It emphasizes:

- **Client-Side Encryption**: All cryptographic operations, including message and file encryption, are performed **on the client side** using .NET cryptography libraries. The server stores only encrypted data.

- **End-to-End Type Safety**: Using **Carter** for route definitions, **FluentValidation** for validation, and **OneOf** or **LanguageExt** for ADTs ensures strong typing and compile-time safety across the application.

- **Functional Programming Practices**: Adopting immutable data structures, pattern matching, and pure functions where possible, leveraging modern C# features.

- **Modern Features**: Utilizing C# 9.0+ features like **records**, **pattern matching enhancements**, and **nullable reference types** to write cleaner and safer code.

- **Shared Code**: By sharing models and utilities between the client and server, the system ensures consistency and reduces code duplication.

- **Real-Time Communication**: Using **SignalR** simplifies the implementation of real-time features, providing a robust and scalable solution with strong typing.

- **SQL Schema Inclusion**: The SQL statements for setting up the database are included, providing a complete and explicit database schema.