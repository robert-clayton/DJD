using Godot;
using System;

public class SlimeFire : Enemy{
    Particles2D fireEmitter = new Particles2D();
    ParticlesMaterial fireMaterial = ResourceLoader.Load("res://particles/burn.tres") as ParticlesMaterial;

    public SlimeFire() : base(){
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

        fireEmitter.Amount = 25;
        fireEmitter.ProcessMaterial = fireMaterial;
        AddChild(fireEmitter);
        
    }
    

    public override void _Process(float delta){
        base._Process(delta);
    }

    public override void PrepareDeath(){
        base.PrepareDeath();
        fireEmitter.Emitting = false;
    }
}
