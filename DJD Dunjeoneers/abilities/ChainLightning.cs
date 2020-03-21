using Godot;
using System;
using System.Collections.Generic;

public class ChainLightning : AbilityBase
{
    private Timer jumpDelay = new Timer();
    private List<(Entity, Particles2D, Particles2D, Light2D, Timer)> particleSets = new List<(Entity, Particles2D, Particles2D, Light2D, Timer)>();
    private ParticlesMaterial chain = ResourceLoader.Load("res://particles/lightning_chain.tres") as ParticlesMaterial;
    private ParticlesMaterial crackle = ResourceLoader.Load("res://particles/crackle.tres") as ParticlesMaterial;
    private float maxJumpLength = 50f;
    private Vector2 startingPosition;

    public ChainLightning() : base(){}

    public ChainLightning(Vector2 _direction, Vector2 _pos, int _targetLayer) : base(_direction, _pos, _targetLayer){
        maxPierces = 6;
        damage = 50f;
        jumpDelay.WaitTime = .1f;
        jumpDelay.OneShot = true;
        AddChild(jumpDelay);
        jumpDelay.Connect("timeout", this, nameof(Jump));
    }

    public override void _Ready(){
        startingPosition = Position;
        Jump();
    }

    public override void _Process(float delta){
        UpdateParticleSets(particleSets);
    }

    public void Jump(){
        if (!IsInstanceValid(GetTree()) || pierces++ >= maxPierces) return;
        (Entity entity, float distance) closest = (null, -1);
        foreach (Entity entity in GetTree().GetNodesInGroup("Enemies")){
            bool cont = false;
            foreach ((Entity entity, Particles2D, Particles2D, Light2D, Timer) set in particleSets){
                if (set.entity == entity){
                    cont = true;
                    break;
                }
            }
            if (cont) continue;
            if (closest.entity == null || Position.DistanceTo(entity.Position) < closest.Item2){
                closest = (entity, Position.DistanceTo(entity.Position));
            }
        }
        if (closest.entity != null && closest.distance < maxJumpLength){
            var nodes = MakeLightningParticles(Position, closest.Item1.Position);
            particleSets.Add((closest.entity, nodes.Item1, nodes.Item2, nodes.Item3, nodes.Item4));
            Position = closest.entity.Position;
            closest.entity.Damage(damage);
            jumpDelay.Start();
        }
    }

    private (Particles2D, Particles2D, Light2D, Timer) MakeLightningParticles(Vector2 from, Vector2 to){
        Particles2D chainEmitter = MakeChainParticle(from, to);
        GetTree().Root.AddChild(chainEmitter);

        Particles2D crackleEmitter = MakeCrackleParticle(to);
        GetTree().Root.AddChild(crackleEmitter);

        Light2D light = new Light2D();
        Texture gradient = ResourceLoader.Load("res://assets/gradients/radial.png") as Texture;
        light.Position = to;
        light.Texture = gradient;
        light.Scale = new Vector2(.1f, .1f);
        light.Color = new Color(.4f, .4f, 1f, .2f);
        GetTree().Root.AddChild(light);

        Timer timer = new Timer();
        timer.OneShot = true;
        timer.WaitTime = .5f;
        GetTree().Root.AddChild(timer);
        timer.Start();
        return (chainEmitter, crackleEmitter, light, timer);
    }

    private Particles2D MakeChainParticle(Vector2 from, Vector2 to){
        Particles2D chainEmitter = new Particles2D();
        ParticlesMaterial material = chain.Duplicate() as ParticlesMaterial;
        material.EmissionBoxExtents = new Vector3(from.DistanceTo(to) / 2, 0, 0);
        chainEmitter.Position = (from + to) / 2;
        chainEmitter.ProcessMaterial = material;
        chainEmitter.Amount = Mathf.Max(10 * (int)from.DistanceTo(to), 1);
        chainEmitter.Rotate(from.DirectionTo(to).Angle());
        chainEmitter.SpeedScale = 5;
        chainEmitter.Lifetime = 1f;
        return chainEmitter;
    }

    private void UpdateParticleSets(List<(Entity entity, Particles2D chainEmitter, Particles2D crackleEmitter, Light2D light, Timer timer)> sets){
        // Update set to only have valid entities
        for (int k = sets.Count - 1; k >= 0; k--){
            if (!IsInstanceValid(sets[k].entity) || sets[k].timer.TimeLeft == 0f){
                sets[k].chainEmitter.QueueFree();
                sets[k].crackleEmitter.QueueFree();
                sets[k].light.QueueFree();
                sets[k].timer.QueueFree();
                sets.RemoveAt(k);
            }
        }

        // loop through emitters and update values
        for (int idx = 0; idx < sets.Count; idx++){
            var from = idx == 0? startingPosition : sets[idx-1].entity.Position;
            var to = sets[idx].entity.Position;

            ParticlesMaterial material = sets[idx].chainEmitter.ProcessMaterial as ParticlesMaterial;
            material.EmissionBoxExtents = new Vector3(from.DistanceTo(to) / 2, 0, 0);
            sets[idx].chainEmitter.Position = (from + to) / 2;
            sets[idx].chainEmitter.ProcessMaterial = material;
            sets[idx].chainEmitter.Amount = Mathf.Max(10 * (int)from.DistanceTo(to), 1);
            sets[idx].chainEmitter.Rotate(from.DirectionTo(to).Angle() - sets[idx].chainEmitter.Rotation);
            sets[idx].light.Position = to;
            sets[idx].crackleEmitter.Position = to;
        }
    }

    private Particles2D MakeCrackleParticle(Vector2 pos){
        Particles2D crackleEmitter = new Particles2D();
        ParticlesMaterial material = crackle.Duplicate() as ParticlesMaterial;
        crackleEmitter.ProcessMaterial = material;
        crackleEmitter.Amount = 150;
        crackleEmitter.SpeedScale = 2f;
        crackleEmitter.Position = pos;
        return crackleEmitter;
    }

    public override void _PhysicsProcess(float delta){
    //     var toDelete = new List<(Particles2D, Particles2D, Light2D, Timer)>();
    //     foreach ((Particles2D chainEmitter, Particles2D crackleEmitter, Light2D light, Timer timer) set in particleSets){
    //         if (set.timer.TimeLeft / set.timer.WaitTime < .1f)
    //             toDelete.Add(set);
    //     }
    //     foreach((Particles2D chainEmitter, Particles2D crackleEmitter, Light2D light, Timer timer) set in toDelete){
    //         particleSets.Remove(set);
    //         set.chainEmitter.QueueFree();
    //         set.crackleEmitter.QueueFree();
    //         set.light.QueueFree();
    //     }
    //     if (toDelete.Count > particleSets.Count) QueueFree();
    }
}
