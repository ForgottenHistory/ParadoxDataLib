# ParadoxDataLib

A .NET Standard 2.1 library for parsing and working with Paradox Interactive game data files.

## Overview

ParadoxDataLib provides utilities for parsing data files from Paradox Interactive games like Europa Universalis IV, Crusader Kings, Hearts of Iron, and others. The library is designed for high performance and supports modern C# language features.

Tested with EU IV files.

## Features

- **High Performance**: Optimized parsing with unsafe code blocks for maximum speed
- **Modern C#**: Built with C# 10.0 and nullable reference types enabled
- **Cross Platform**: Targets .NET Standard 2.1 for broad compatibility
- **JSON Support**: Includes System.Text.Json for modern JSON handling
- **Async Operations**: Built-in support for async operations with System.Threading.Channels

## Getting Started

### Prerequisites

- .NET Standard 2.1 compatible runtime
- Visual Studio 2017 or later (for development)

### Building

```bash
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Running Benchmarks

```bash
dotnet run --project ParadoxDataLib.Benchmarks
```

## Project Structure

- `ParadoxDataLib/` - Main library source code
- `ParadoxDataLib.Tests/` - Unit tests
- `ParadoxDataLib.Benchmarks/` - Performance benchmarks
- `docs/` - Documentation
- `examples/` - Usage examples

## Documentation

- [Modding Guide](MODDING_GUIDE.md) - Guide for modding Paradox games
- [Performance Guidelines](PERFORMANCE_GUIDELINES.md) - Best practices for optimal performance

## Dependencies

- System.Text.Json (8.0.4)
- System.Threading.Channels (8.0.0)
