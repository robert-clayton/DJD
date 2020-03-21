using Godot;
using System;

public class BlastWave : AbilityBase
{

    public BlastWave() : base(){}

    public BlastWave(Vector2 direction, Vector2 pos, int targetLayer) : base(direction, pos, targetLayer){
    }

    public override void _PhysicsProcess(float delta){

    }
}
