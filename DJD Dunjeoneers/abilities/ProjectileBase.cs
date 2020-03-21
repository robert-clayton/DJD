using Godot;
using System;

public abstract class ProjectileBase : AbilityBase{


    public ProjectileBase(){}

    public ProjectileBase(Vector2 direction, Vector2 position, int targetLayer) : base(direction, position, targetLayer){}

    public override void _PhysicsProcess(float delta){
        base._PhysicsProcess(delta);
        Translate(InitialVelocity * delta);
    }
}
