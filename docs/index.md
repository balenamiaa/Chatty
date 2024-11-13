# Chatty Documentation

## Introduction

Chatty is a secure, real-time messaging platform built with ASP.NET Core, featuring end-to-end encryption, voice/video calls, and comprehensive server management capabilities. This documentation provides a deep dive into the architecture, design decisions, and implementation details of the system.

## Table of Contents

1. [System Architecture](architecture/system-overview.md)
   - Core Architecture
   - Key Components
   - Design Decisions
   - Integration Points

2. [Security](security/overview.md)
   - End-to-End Encryption
   - Authentication & Authorization
   - Key Management
   - Device Verification
   - Data Protection

3. [Real-time Communication](realtime/overview.md)
   - WebSocket Architecture
   - Event System
   - Presence Management
   - Voice/Video Integration
   - Message Delivery

4. [Data Management](data/overview.md)
   - Database Design
   - Data Flow
   - Query Optimization
   - Data Protection
   - Migration Strategy

5. [API Design](api/overview.md)
   - REST Endpoints
   - Validation
   - Error Handling
   - Rate Limiting
   - Documentation

6. [Background Processing](background/overview.md)
   - Maintenance Tasks
   - Cleanup Operations
   - Health Monitoring
   - Performance Optimization

## Quick Start

### Prerequisites
- .NET 9.0 SDK
- PostgreSQL 15+

### Getting Started
1. Clone the repository
2. Configure your environment
3. Run database migrations
4. Start the application

For detailed setup instructions, see the [Getting Started Guide](getting-started.md).

## Architecture Overview

Chatty follows a modern, secure architecture with these key characteristics:

### Security First
- End-to-end encryption for all messages and files
- Zero-knowledge architecture
- Strong authentication and authorization
- Secure key management

### Real-time Communication
- WebSocket-based messaging
- Presence system
- Voice and video calls
- Push notifications

### Scalable Design
- Modular architecture
- Event-driven communication
- Background processing
- Performance optimization

## Contributing

See our [Contributing Guide](contributing.md) for details on:
- Code style
- Pull request process
- Testing requirements
- Documentation updates

## Further Reading

- [API Documentation](api/reference.md)
- [Security Model](security/model.md)
- [Deployment Guide](deployment/guide.md)
- [Troubleshooting](support/troubleshooting.md) 