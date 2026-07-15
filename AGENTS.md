# About this project

## Overview

This monorepo's main components:

- `Packages/com.unity.cloud.gltfast` - Unity package for import/export of glTF 3D assets
- `Packages/com.unity.cloud.gltfast.tests` - Unit and integration tests
- `Projects/` - Contains multiple Unity projects with different setups for development and testing
  - `Projects/glTFast-Test` - main test project

## Code style guidelines

- C# version: 9.0
- Never create `.meta` files directly. The Unity Editor will create them automatically
- Prefer state return values over C# exceptions (that may not work in Web builds)
- Once code modifications are complete, ensure correct code format (as depicted by `.editorconfig`) by running `dotnet format Projects/glTFast-Test/glTFast-Test.sln`
- Follow the .NET Framework Design Guidelines
- Code comments: write none by default. Only add one when the code can't be understood by reading it (hidden constraint, non-obvious invariant, workaround, surprising behavior).
  - Not for design rationale, restating what the code does, or task/fix/caller refs.
  - XML doc comments (`///`) on public API are documentation, not commentary.
- `Unity.Gltfast.Text.Json` is a copy of `System.Text.Json` version 10 used as a drop-in replacement for JSON deserialization.

### Performance

Optimize code for fast execution and minimize the amount of managed memory allocated.

- Prefer using `NativeCollection` containers such as `NativeArray` over managed containers.
- Parallelize data processing
  - Prefer the C# job system (`IJob`)
  - Alternatively use `System.Threading`, minding the special behavior of `UnitySynchronizationContext`
- Use the Burst compiler (`BurstCompileAttribute`) where applicable
