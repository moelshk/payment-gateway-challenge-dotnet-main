# Design Considerations and Assumptions

## Key Design Decisions

### Architecture
- **Layered Architecture**: Separated concerns into Controllers, Services, and Repository layers for maintainability and testability
- **Dependency Injection**: Used ASP.NET Core's built-in DI container for loose coupling
- **Interface-based Design**: Introduced interfaces (`IPaymentService`, `IBankSimulatorClient`) to enable testing and future extensibility

### Validation Strategy
- **Client-side Validation**: Validation is performed at the API layer before calling the bank simulator, ensuring invalid requests are rejected without making unnecessary external calls
- **Custom Validation Attributes**: Created `FutureExpiryDateAttribute` and `AllowedCurrenciesAttribute` for reusable validation logic
- **Currency Limitation**: Limited to 3 ISO currencies (USD, GBP, EUR) as per requirements

### Error Handling
- **Rejected Status**: When validation fails or bank returns errors (503), payment is marked as "Rejected" and stored with all payment details for audit purposes
- **Status Differentiation**: Distinguishes between validation failures (no bank call) and bank errors (bank called but failed)

### Storage
- **In-Memory Repository**: Used `ConcurrentDictionary` for thread-safe in-memory storage as per requirements (no real database needed)
- **Payment Persistence**: All payments (including rejected ones) are stored to enable retrieval and audit trail

### Payment Status Flow
- **Authorized**: Bank successfully authorized (card ending in odd digit)
- **Declined**: Bank declined the payment (card ending in even digit)
- **Rejected**: Validation failed (bank not called) or bank error occurred (503 Service Unavailable)

## Assumptions

### Business Logic
- Payment IDs are generated as GUIDs for uniqueness
- Only the last 4 digits of card numbers are returned for compliance
- Expiry date validation checks that the full month/year combination is in the future
- Amount is always in minor currency units (e.g., cents for USD)

### Bank Simulator Integration
- Bank simulator is always available at `http://localhost:8080` during development
- All bank errors (including 503) result in "Rejected" status
- Bank responses are synchronous - no retry logic implemented

### Currency Support
- Only 3 currencies are supported initially (USD, GBP, EUR) as per requirements
- Currency codes are case-insensitive (validated with `.ToUpper()`)

### Validation Rules
- Card numbers must be exactly 14-19 digits (no spaces or dashes)
- Expiry date format is strict MM/YYYY
- CVV must be exactly 3 or 4 digits

## Trade-offs

### Simplicity Over Features
- Chose simple in-memory storage over database for faster development and testing
- No authentication/authorization implemented (not required)
- Minimal error handling to focus on core functionality

### Synchronous Processing
- Payment processing is synchronous - no queue or async processing implemented
- Suitable for the initial phase but may need async processing for scale

