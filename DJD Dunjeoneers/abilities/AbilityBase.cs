using Godot;
using System;

public abstract class AbilityBase : Node2D{
    public Vector2 Velocity {get; set;} = new Vector2();
    public Vector2 InitialVelocity {get; set;} = new Vector2();
    public Vector2 Direction {get; set;} = new Vector2();
    public float LifetimePercent {
        get{
            if (IsInstanceValid(lifetime) && lifetime.WaitTime > 0f)
                return 1 - lifetime.TimeLeft / lifetime.WaitTime;
            return 0f;
        }
    }
    public float LifetimeTotal {
        get { return lifetime.WaitTime; }
        set { lifetime.WaitTime = value; }
    }

    public float Acceleration {get; set;} = 100f;
    public Timer lifetime = new Timer();
    public Tween initVelDecelerationTween = new Tween();
    protected int targetLayer = -1;
    protected float knockbackStrength = 50f;
    protected int maxPierces = 0;
    protected int pierces = 0;
    protected float damage = 25f;
    protected float effectRadius = 0f;
    public float cooldown = 0.5f;


    public AbilityBase(){}

    public AbilityBase(Vector2 direction, Vector2 position, int targetLayer){
        lifetime.OneShot = true;
        lifetime.Autostart = true;

        Direction = direction;
        Position = position;
        this.targetLayer = targetLayer;
    }

    public override void _Ready(){
        AddChild(lifetime);
        AddChild(initVelDecelerationTween);
        initVelDecelerationTween.InterpolateProperty(this, nameof(InitialVelocity), InitialVelocity, default(Vector2), 0.5f);
        initVelDecelerationTween.Start();
    }

    public Vector2 AccelerateExponential(Vector2 currentVelocity, int power = 2){
        return currentVelocity + (Direction * Mathf.Pow(LifetimePercent, power) * Acceleration);
    }

    public Vector2 AccelerateLogarithmic(Vector2 currentVelocity){
        return currentVelocity + Direction * Mathf.Log(1 + LifetimePercent * Acceleration);
    }
}
