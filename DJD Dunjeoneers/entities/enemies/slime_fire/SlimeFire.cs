using Godot;
using System;

public class SlimeFire : Enemy{
    private Particles2D _fireEmitter = new Particles2D();
    private ParticlesMaterial _fireMaterial = ResourceLoader.Load("res://particles/burn.tres") as ParticlesMaterial;

    public SlimeFire(Vector2 _position, Vector2 _moveAreaStart, Vector2 _moveAreaEnd) : base(_position, _moveAreaStart, _moveAreaEnd){
        maxEnergy = 100f;
        curEnergy = 100f;
        maxHealth *= 2;
        curHealth *= 2f;
        knockbackCutoff = 4f;
        knockbackDeceleration = 5f;
        acceleration = 5f;
        maxVelocity = 35f;

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
