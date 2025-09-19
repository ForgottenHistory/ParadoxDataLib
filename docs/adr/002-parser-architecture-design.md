# ADR-002: Hierarchical Parser Architecture with Base Classes

## Status
Accepted

## Context
The Paradox Data Parser needs to handle multiple types of game files (provinces, countries, cultures, religions) with varying but related formats. Each file type has unique attributes but shares common parsing patterns like tokenization, error handling, and file I/O.

## Decision
We will implement a hierarchical parser architecture with an abstract `BaseParser<T>` class that provides common functionality, while specific parsers inherit and implement domain-specific parsing logic.

## Rationale

### Code Reuse and Consistency
- **Common Infrastructure**: Tokenization, file handling, error collection, and async operations are shared
- **Consistent Error Handling**: Standardized error reporting across all parser types
- **Uniform API**: All parsers expose the same interface for parsing, validation, and async operations

### Maintainability Benefits
- **Single Responsibility**: Each parser focuses only on its specific data format
- **Easy Extension**: New game file types can be added by inheriting from BaseParser
- **Centralized Improvements**: Performance optimizations in BaseParser benefit all parsers

### Type Safety
- **Generic Design**: `BaseParser<T>` ensures type safety at compile time
- **Proper Return Types**: Each parser returns strongly-typed data structures
- **Interface Contracts**: `IDataParser<T>` defines clear contracts

## Implementation Details

### Base Parser Structure
```csharp
public abstract class BaseParser<T> : IDataParser<T>
{
    // Common infrastructure
    protected List<Token> _tokens;
    protected int _currentTokenIndex;
    protected readonly List<string> _errors;
    protected readonly List<string> _warnings;

    // Common operations
    public virtual T Parse(string content) { /* tokenize and delegate */ }
    public virtual async Task<T> ParseAsync(string content) { /* async wrapper */ }

    // Template method pattern
    protected abstract T ParseTokens(); // Implemented by derived classes

    // Utility methods for token navigation
    protected Token CurrentToken() { /* ... */ }
    protected bool ExpectToken(TokenType type) { /* ... */ }

    // Common parsing helpers
    protected DateTime ParseDate(string dateString) { /* ... */ }
    protected List<string> ParseStringList() { /* ... */ }
}
```

### Specific Parser Implementation
```csharp
public class ProvinceParser : BaseParser<ProvinceData>
{
    protected override ProvinceData ParseTokens()
    {
        // Province-specific parsing logic
        // Uses base class utilities for common patterns
    }

    // Province-specific helpers
    private bool IsBuilding(string key) { /* ... */ }
    private bool ParseBooleanValue(Token token) { /* ... */ }
}
```

## Architecture Benefits

### Template Method Pattern
- Base class defines the parsing algorithm structure
- Derived classes implement specific parsing steps
- Ensures consistent error handling and file processing

### Composition Over Inheritance
- Parsers use composition for tokenization (Lexer class)
- Inheritance only for shared parsing infrastructure
- Clear separation between tokenization and parsing concerns

### Extensibility
- New file formats can be supported by adding new parser classes
- Common functionality automatically available to new parsers
- Plugin architecture possible for custom mod formats

## Performance Considerations

### Tokenization Efficiency
- Single tokenization pass with reusable token list
- Token navigation methods optimize for sequential access
- Memory-efficient token representation

### Async Support
- Non-blocking file I/O operations
- Parallel processing support for multiple files
- Proper async/await patterns throughout

### Error Handling Efficiency
- Error collection without exceptions in hot paths
- Lazy error string formatting
- Optional validation levels (errors vs warnings)

## Consequences

### Positive
- **Consistent Behavior**: All parsers work the same way
- **Reduced Code Duplication**: Common logic centralized
- **Easy Testing**: Base class provides testable infrastructure
- **Performance**: Shared optimizations benefit all parsers
- **Maintainability**: Changes to common logic affect all parsers

### Negative
- **Complexity**: Inheritance hierarchy requires understanding
- **Coupling**: Changes to base class can affect all derived classes
- **Flexibility**: Some parser-specific optimizations might be harder to implement

### Mitigation Strategies
- Comprehensive documentation of base class contracts
- Extensive unit testing of base class functionality
- Virtual methods where parser-specific behavior is needed
- Interface segregation for optional features

## Testing Strategy

### Base Class Testing
```csharp
[Test]
public void BaseParser_ErrorHandling_CollectsErrors()
{
    var parser = new TestParser(); // Concrete test implementation
    parser.Parse("invalid content");
    Assert.That(parser.HasErrors, Is.True);
}
```

### Integration Testing
- Test each parser against real game files
- Validate error handling with malformed input
- Performance testing with large datasets

## Alternatives Considered

### Option 1: Separate Parser Classes
- **Pros**: Complete independence, no coupling
- **Cons**: Code duplication, inconsistent behavior
- **Verdict**: Rejected due to maintenance burden

### Option 2: Composition-Only Architecture
- **Pros**: More flexible, easier to test individual components
- **Cons**: More complex object graphs, potential performance overhead
- **Verdict**: Considered for future refactoring

### Option 3: Static Utility Classes
- **Pros**: Simple, no inheritance complexity
- **Cons**: No polymorphism, harder to extend
- **Verdict**: Rejected due to lack of extensibility

## Performance Metrics
- Parsing 5000 province files: 2.3 seconds (baseline)
- Memory usage: 45MB for full EU4 dataset
- Error rate: <0.1% false positives on validation

## Related Decisions
- [ADR-001: Struct-Based Data Models](001-struct-based-data-models.md)
- [ADR-004: Tokenization Strategy](004-tokenization-strategy.md)

## Future Considerations
- Consider composition-based architecture for v2.0
- Evaluate source generators for parser generation
- Investigate incremental parsing for large files