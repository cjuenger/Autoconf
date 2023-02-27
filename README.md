# Autoconf

**Autoconf** is a very small extension library for **Autofac**. It allows to register _specialized_ configuration classes at Autofac's ContainerBuilder seamlessly.

## 🌐 Features

- Register a _specialized_ configuration class via `RegisterConfiguration()` at the `ContainerBuilder`.
  - _Example:_
    ``` csharp
    containerBuilder
      .RegisterConfiguration<MyTestConfig>()
      .As<IMyTestConfig>();
    ```
- Log the registered configuration

## 🛠️ Requirements

- Requires `ILogger` (namespace: `Microsoft.Extensions.Logging`) to be registered.
- Requires `IConfiguration` (namespace: `Microsoft.Extensions.Configuration`) to be registered.