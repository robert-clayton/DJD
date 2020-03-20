using Godot;
using System;

public class SlimeBasic : Enemy{
    public SlimeBasic(Vector2 _position, Vector2 _moveAreaStart, Vector2 _moveAreaEnd) : base(_position, _moveAreaStart, _moveAreaEnd){
        knockbackCutoff = 4f;
        knockbackDeceleration = 5f;
        Acceleration = 4f;
        MaxVelocity = 25f;

        // Children
        sprite.Frames = ResourceLoader.Load("res://entities/enemies/slime_basic/SlimeBasic.tres") as SpriteFrames;
        ChangeState(EEnemyState.STATE_IDLE);
    }

    public override void _Process(float delta){
        base._Process(delta);
    }
}
