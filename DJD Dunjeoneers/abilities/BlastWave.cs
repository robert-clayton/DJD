using Godot;
using System;

public class BlastWave : AbilityBase
{

    public BlastWave() : base(){}

    public BlastWave(Vector2 _direction, Vector2 _pos, int _targetLayer) : base(_direction, _pos, _targetLayer: _targetLayer){

        
    }

    public override void _PhysicsProcess(float delta){

    }
}
