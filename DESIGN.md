# Design Considerations and Assumptions

## Design Decisions

### Architecture
- Simple layered structure: Controllers handle HTTP, Services contain business logic, Repository handles storage
- Used dependency injection for testability
- Interfaces (`IPaymentService`, `IBankSimulatorClient`) enable mocking in tests

### Validation
- Validation performed at controller level before calling bank simulator
- Invalid requests are rejected without calling the bank (as per requirements)
- Custom validation attributes for expiry date (must be in future) and currency (USD, GBP, EUR only)

### Payment Status
- **Authorized**: Bank authorized payment (card ending in odd digit)
- **Declined**: Bank declined payment (card ending in even digit)
- **Rejected**: Invalid information (bank not called) or bank error (503)

### Storage
- In-memory `ConcurrentDictionary` for thread-safe storage (as per requirements - no database needed)
- All payments stored regardless of status for retrieval

## Assumptions

- Payment IDs use GUID format
- Only last 4 digits of card number returned (PCI compliance)
- Amount always in minor currency units (e.g., 100 = $1.00 USD)
- Bank simulator at `http://localhost:8080`
- Bank errors (503, 400) result in "Rejected" status
- Currency validation is case-insensitive
- Expiry date format: MM/YYYY, must be in future
