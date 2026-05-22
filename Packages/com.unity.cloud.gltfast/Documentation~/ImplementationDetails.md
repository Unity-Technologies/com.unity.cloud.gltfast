# Implementation Details

*Unity glTFast* uses [System.Text.Json](https://www.nuget.org/packages/system.text.json/)'s source-generation-backed JSON (de-)serialization, which provides great speed, minimizes memory allocation overhead and offers comprehensive JSON access for advanced customizations.

*glTFast* also uses fast low-level memory copy methods, the [C# Job System](https://docs.unity3d.com/Manual/JobSystem.html), [Mathematics](https://docs.unity3d.com/Packages/com.unity.mathematics@1.0/manual/index.html), the [Burst compiler](https://docs.unity3d.com/Packages/com.unity.burst@1.6/manual/index.html) and the [Advanced Mesh API](https://docs.unity3d.com/ScriptReference/Mesh.html) to optimize loading times.

## Trademarks

*Unity&reg;* is a registered trademark of [Unity Technologies][unity].

*Khronos&reg;* is a registered trademark and *glTF&trade;* is a trademark of [The Khronos Group Inc][khronos].

[khronos]: https://www.khronos.org
[unity]: https://unity.com
