using Godot;
using System;

public class SlimeFire : Enemy{
    private Particles2D _fireEmitter = new Particles2D();
    private ParticlesMaterial _fireMaterial = ResourceLoader.Load("res://particles/burn.tres") as ParticlesMaterial;

    public SlimeFire() : base(){}

    public override void Initialize(Vector2 position, int size = 8){
        base.Initialize(position, size: size);
        MaxHealth *= 2f;
        CurHealth *= 2f;
        knockbackCutoff = 4f;
        knockbackDeceleration = 5f;
        Acceleration = 5f;
        MaxVelocity = 35f;

        // Children
        sprite.Frames = ResourceLoader.Load("res://entities/enemies/slime_fire/SlimeFire.tres") as SpriteFrames;
        ChangeState(EEnemyState.STATE_IDLE);

        _fireEmitter.Amount = 25;
        _fireEmitter.ProcessMaterial = _fireMaterial;
        AddChild(_fireEmitter);
    }
    

    public override void _Process(float delta){
        base._Process(delta);
    }

    protected override void PrepareDeath(){
        base.PrepareDeath();
        _fireEmitter.Emitting = false;
    }
}
