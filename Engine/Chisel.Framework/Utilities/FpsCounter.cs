using System;
using System.Collections.Generic;

namespace Chisel.Framework;

public class FpsCounter
{
    public double AverageFps { get; private set; }
    private double _accumulatedTime;
    private readonly Queue<double> _frameTimes = new();

    public void Update(double delta)
    {
        _frameTimes.Enqueue(delta);
        _accumulatedTime += delta;

        while (_accumulatedTime > 1.0 && _frameTimes.Count > 1)
        {
            _accumulatedTime -= _frameTimes.Dequeue();
        }

        if (_accumulatedTime > 0.0)
        {
            AverageFps = _frameTimes.Count / _accumulatedTime;
        }
    }
}