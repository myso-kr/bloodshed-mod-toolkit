// CI 컴파일 전용 스텁 — 런타임에서는 BepInEx/interop/Unity.Entities.dll 등이 사용됩니다.
// csproj의 <Compile Remove="Stubs\**"> 로 로컬 빌드에서는 자동 제외됩니다.
using System;

// ── Unity.Mathematics ────────────────────────────────────────────────────────
namespace Unity.Mathematics
{
    public struct float3
    {
        public float x, y, z;
        public float3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static float3 zero => new float3(0, 0, 0);
    }
}

// ── Unity.Collections ────────────────────────────────────────────────────────
namespace Unity.Collections
{
    public enum Allocator { Invalid = 0, None, Temp, TempJob, Persistent }

    public struct NativeArray<T> : IDisposable where T : struct
    {
        private readonly T[] _data;
        public NativeArray(int length, Allocator allocator) { _data = new T[length]; }
        public T   this[int index] => _data[index];
        public int Length          => _data?.Length ?? 0;
        public void Dispose()      { }
    }
}

// ── Unity.Transforms ─────────────────────────────────────────────────────────
namespace Unity.Transforms
{
    using Unity.Mathematics;

    public struct LocalTransform
    {
        public float3    Position;
        public float     Scale;
        public static LocalTransform Identity => new LocalTransform { Scale = 1f };
    }

    public struct LocalToWorld
    {
        public float3 Position;
    }
}

// ── Unity.Entities ────────────────────────────────────────────────────────────
namespace Unity.Entities
{
    using Unity.Collections;
    using Unity.Transforms;

    public struct Entity : IEquatable<Entity>
    {
        public int Index;
        public int Version;
        public bool Equals(Entity other) => Index == other.Index && Version == other.Version;
        public override bool Equals(object? obj) => obj is Entity e && Equals(e);
        public override int  GetHashCode()        => Index;
        public static readonly Entity Null = new Entity { Index = -1, Version = 0 };
    }

    public struct ComponentType
    {
        public int TypeIndex;
        public static ComponentType ReadOnly<T>() => new ComponentType();
        public static ComponentType ReadWrite<T>() => new ComponentType();
    }

    public struct EntityQuery : IDisposable
    {
        public NativeArray<Entity> ToEntityArray(Allocator allocator) => new NativeArray<Entity>(0, allocator);
        public int                 CalculateEntityCount() => 0;
        public void                Dispose() { }
    }

    public struct EntityManager
    {
        public EntityQuery CreateEntityQuery(params ComponentType[] componentTypes) => new EntityQuery();
        public T           GetComponentData<T>(Entity entity)        where T : struct => default;
        public void        SetComponentData<T>(Entity entity, T value) where T : struct { }
        public bool        HasComponent<T>(Entity entity)             where T : struct => false;
        public bool        Exists(Entity entity) => false;
        public void        CompleteAllTrackedJobs() { }
    }

    public class World : IDisposable
    {
        public static World? DefaultGameObjectInjectionWorld => null;
        public bool           IsCreated    => false;
        public EntityManager  EntityManager => default;
        public void           Dispose()     { }
    }
}
