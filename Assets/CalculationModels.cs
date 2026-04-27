public sealed class CalculationInput
{
    public CalculationInput(float bht, float tms, float td, float d, float ri, float rmf, float rm, float h, float psp, float sp)
    {
        BHT = bht;
        Tms = tms;
        Td = td;
        D = d;
        Ri = ri;
        Rmf = rmf;
        Rm = rm;
        H = h;
        PSP = psp;
        SP = sp;
    }

    public float BHT { get; }
    public float Tms { get; }
    public float Td { get; }
    public float D { get; }
    public float Ri { get; }
    public float Rmf { get; }
    public float Rm { get; }
    public float H { get; }
    public float PSP { get; }
    public float SP { get; }
}

public sealed class CalculationResult
{
    public CalculationResult(float tf, float rw, float vsh)
    {
        Tf = tf;
        Rw = rw;
        Vsh = vsh;
    }

    public float Tf { get; }
    public float Rw { get; }
    public float Vsh { get; }
}
