## Little helper for running

### Terminal 1 - Setup our NATS server
```nats-server -js -m 8222 -sd ./nats_store```

### Terminal 2 - Run our main system
```dotnet run --project ./src/TOOL/```

### Terminal 3 - Run the seeding script
```dotnet run --project ./src/TOOL/ --seed-only```

### Terminal 4 - Run the agent container
```dotnet run --project ./src/Agent.Container```

### Terminal 5 - Run benchmarks with
```dotnet test tests/TOOL.Tests/TOOL.Tests.csproj --filter "Benchmark"```