using Godot;
using System;

public class Pyroblast : AbilityBase{
    private bool exploded = false;
    private Timer explosionCompleteTimer = new Timer();
    private Timer initialBoomVelocityTimer = new Timer();
    private Texture spriteImage = ResourceLoader.Load("res://assets/sprites/fireball.png") as Texture;
    private ParticlesMaterial explosionMaterial = ResourceLoader.Load("res://particles/explosion.tres") as ParticlesMaterial;
    private ParticlesMaterial smokeTrailMaterial = ResourceLoader.Load("res://particles/smoke_trail.tres") as ParticlesMaterial;
    private ParticlesMaterial fireTrailMaterial = ResourceLoader.Load("res://particles/fire_trail.tres") as ParticlesMaterial;
    private Sprite sprite = new Sprite();
    private Particles2D explosionEmitter = new Particles2D();
    private Particles2D smokeTrailEmitter = new Particles2D();
    private Particles2D fireTrailEmitter = new Particles2D();
    private Area2D hitArea = new Area2D();

    public Pyroblast() : base(){}

    public Pyroblast(Vector2 _direction, Vector2 _pos, int _targetLayer) : base(_direction, _pos, _targetLayer){
        maxPierces = 3; 
        damage = 1000f;
        knockbackStrength = 200f;
        maxVelocity = 100; 
        acceleration = 10;
        effectRadius = 30f;
        cooldown = 3f;

        Position = Position + direction * 15f;
        Rotate(GetAngleTo(Position + direction));

        VisibilityNotifier2D onScreenNotifier = new VisibilityNotifier2D();
        onScreenNotifier.Connect("screen_exited", this, "queue_free");
        AddChild(onScreenNotifier);

        // Sprite
        sprite.Texture = spriteImage;
        AddChild(sprite);
    
        // Collider
        hitArea.SetCollisionMaskBit(targetLayer, true);
        AddChild(hitArea);
        CollisionShape2D shape = new CollisionShape2D();
        shape.Transform = new Godot.Transform2D(0f, new Vector2(-1, 0f));
        CircleShape2D circle = new CircleShape2D();
        circle.Radius = 6;
        shape.Shape = circle;
        hitArea.AddChild(shape);
        hitArea.Connect("area_entered", this, "_OnHurtAreaEnter");

        // Emitters
        explosionEmitter.Emitting = false;
        explosionEmitter.Amount = 300;
        explosionEmitter.Lifetime = 4;
        explosionEmitter.OneShot = true;
        explosionEmitter.SpeedScale = 2;
        explosionEmitter.Explosiveness = 1;
        AddChild(explosionEmitter);
        
        smokeTrailEmitter.Amount = 200;
        smokeTrailEmitter.Preprocess = 0.5f;
        smokeTrailEmitter.ProcessMaterial = smokeTrailMaterial;
        AddChild(smokeTrailEmitter);

        fireTrailEmitter.Amount = 200;
        fireTrailEmitter.Preprocess = 0.5f;
        fireTrailEmitter.SpeedScale = 1.5f;
        fireTrailEmitter.Explosiveness = .1f;
        fireTrailEmitter.Randomness = 1;
        fireTrailEmitter.ProcessMaterial = fireTrailMaterial;
        fireTrailEmitter.Transform = new Godot.Transform2D(0f, new Vector2(-1, 0f));
        AddChild(fireTrailEmitter);

        // Timers
        explosionCompleteTimer.WaitTime = 1f;
        explosionCompleteTimer.OneShot = true;
        explosionCompleteTimer.Connect("timeout", this, "queue_free");
        AddChild(explosionCompleteTimer);
        initialBoomVelocityTimer.WaitTime = 0.2f;
        initialBoomVelocityTimer.OneShot = true;
        initialBoomVelocityTimer.Connect("timeout", this, "_RemoveInitialBoomVelocity");
        AddChild(initialBoomVelocityTimer);

        Scale = new Vector2(1.7f, 1.7f);
    }

    public override void _PhysicsProcess(float delta){
        if (!exploded){
            velocity = Mathf.Min(velocity + acceleration, maxVelocity);
            Translate(direction * velocity * delta);
            ZIndex = Mathf.FloorToInt(Position.y);
        }
    }

    public void _OnHurtAreaEnter(Area2D hurtArea){
        // Setup explosion
        if (++pierces >= maxPierces){
            smokeTrailEmitter.Emitting = false;
            smokeTrailEmitter.Scale = new Vector2(0, 0);
            fireTrailEmitter.Emitting = false;
            explosionEmitter.Emitting = true;
            explosionEmitter.ProcessMaterial = explosionMaterial;
            explosionMaterial.Gravity = new Vector3(velocity/2, 0f, 0f);
            exploded = true;
            sprite.Visible = false;
            hitArea.QueueFree();
            explosionCompleteTimer.Start();
            initialBoomVelocityTimer.Start();
        }

        // Affect target(s)
        foreach (Enemy target in GetTree().GetNodesInGroup("Enemies")){
            if (Position.DistanceTo(target.Position) < effectRadius){
                target.Damage(damage, GlobalPosition.DirectionTo(target.GlobalPosition) * knockbackStrength);
            }
        }
    }

    public void _RemoveInitialBoomVelocity(){
        explosionMaterial.Gravity = new Vector3(0f,0f,0f);
    }
}
