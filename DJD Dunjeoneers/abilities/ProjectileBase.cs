using Godot;
using System;

public abstract class ProjectileBase : AbilityBase{
    public Tween initVelDecelerationTween = new Tween();

    public ProjectileBase(){}

    public ProjectileBase(Vector2 direction, Vector2 position, int targetLayer) : base(direction, position, targetLayer){}

    public override void _PhysicsProcess(float delta){
        base._PhysicsProcess(delta);
        Translate(InitialVelocity * delta);
    }

    public override void _Ready(){
        base._Ready();
        AddChild(initVelDecelerationTween);
        initVelDecelerationTween.InterpolateProperty(this, nameof(InitialVelocity), InitialVelocity, MakeInitDecelerationTargetVector(), 0.5f);
        initVelDecelerationTween.Start();
    }

    public Vector2 MakeInitDecelerationTargetVector(){
        float angle = Direction.AngleTo(InitialVelocity.Normalized());
        float magnitude = Mathf.Max(0, Mathf.Cos(angle) * InitialVelocity.Length());
        return magnitude * InitialVelocity.Normalized();
    }
}
