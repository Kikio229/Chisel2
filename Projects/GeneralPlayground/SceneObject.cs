using Chisel.Framework;
using System;

public class SceneObject
{
    public MeshBuffers Mesh;
    public Texture2D Texture;
    public Vector3 Position;
    public Vector3 Scale = Vector3.One;

    public float SpinSpeed;

    public bool Orbits;
    public Vector3 OrbitCenter;
    public float OrbitRadius;
    public float OrbitSpeed;
    public float OrbitPhase;

    public float BobAmplitude;
    public float BobSpeed;

    public float Shininess = 8f;
    public bool Transparent = false;

    public Matrix4 GetWorld(double elapsed)
    {
        Vector3 pos = Position;

        if (Orbits)
        {
            float angle = OrbitPhase + (float)elapsed * OrbitSpeed;
            pos = OrbitCenter + new Vector3(MathF.Cos(angle) * OrbitRadius, pos.Y, MathF.Sin(angle) * OrbitRadius);
        }

        if (BobAmplitude != 0f)
            pos.Y += MathF.Sin((float)elapsed * BobSpeed) * BobAmplitude;

        return Matrix4.FromScale(Scale)
             * Matrix4.FromRotationY((float)elapsed * SpinSpeed)
             * Matrix4.FromTranslation(pos);
    }
}