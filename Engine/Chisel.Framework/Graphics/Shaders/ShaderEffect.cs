using System;
using System.Collections.Generic;

namespace Chisel.Framework;

public class ShaderEffect : IDisposable
{
    Dictionary<string, ShaderPass> techniques = new Dictionary<string, ShaderPass>();
    bool disposedValue;

    public ShaderPass CurrentTechnique { get; private set; }
    public ShaderEffectParameterCollection Parameters { get; }

    internal IEnumerable<ShaderPass> AllPrograms => techniques.Values;

    public ShaderEffect()
    {
        Parameters = new ShaderEffectParameterCollection(this);
    }

    internal void AddTechnique(string name, ShaderPass program)
    {
        techniques[name] = program;

        if (CurrentTechnique == null)
        {
            CurrentTechnique = program;
        }
    }

    public void SetTechnique(string name)
    {
        if (!techniques.TryGetValue(name, out ShaderPass program))
        {
            throw new InvalidOperationException("Shader has no technique named '" + name + "'.");
        }

        CurrentTechnique = program;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                foreach (ShaderPass program in techniques.Values)
                {
                    program.Dispose();
                }
            }
            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}