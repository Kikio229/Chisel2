using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Framework.Utilities;

public sealed class FpsCounter
{
    private readonly Queue<double> frameTimes = new();
    private double accumulatedTime;

    public double AverageFps { get; private set; }

    public void Update(double delta)
    {
        frameTimes.Enqueue(delta);
        accumulatedTime += delta;

        while (accumulatedTime > 1.0 && frameTimes.Count > 1)
        {
            accumulatedTime -= frameTimes.Dequeue();
        }

        if (accumulatedTime > 0.0)
        {
            AverageFps = frameTimes.Count / accumulatedTime;
        }
    }
}