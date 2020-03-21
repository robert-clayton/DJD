using Godot;
using System;

public class SlimeBasic : Enemy{
    public SlimeBasic() : base(){}

    public override void Initialize(Vector2 position, int size = 8){
        base.Initialize(position, size: size);
        MaxHealth = 100f;
        CurHealth = 100f;
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
