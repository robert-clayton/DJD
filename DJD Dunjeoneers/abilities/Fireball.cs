using Godot;
using System;

public class Fireball : ProjectileBase{
    private bool exploded = false;
    private Particles2D explosionEmitter = new Particles2D();
    private Particles2D smokeTrailEmitter = new Particles2D();
    private Particles2D fireTrailEmitter = new Particles2D();
    private ParticlesMaterial explosionMaterial = ResourceLoader.Load("res://particles/explosion.tres").Duplicate() as ParticlesMaterial;
    private ParticlesMaterial smokeTrailMaterial = ResourceLoader.Load("res://particles/smoke_trail.tres") as ParticlesMaterial;
    private ParticlesMaterial fireTrailMaterial = ResourceLoader.Load("res://particles/fire_trail.tres") as ParticlesMaterial;
    private Sprite sprite = new Sprite();
    private Texture spriteImage = ResourceLoader.Load("res://assets/sprites/fireball.png") as Texture;
    
    protected Light2D light = new Light2D();
    private Area2D hitArea = new Area2D();
    public Tween onHitTween = new Tween();

    public Fireball() : base(){}

    public Fireball(Vector2 direction, Vector2 position, int targetLayer) : base(direction, position, targetLayer){
        damage = 100f;
        knockbackStrength = 50f;
        Acceleration = 200;
        effectRadius = 30f;
        LifetimeTotal = 1f;

        Texture gradient = ResourceLoader.Load("res://assets/gradients/radial.png") as Texture;
        light.Texture = gradient;
        light.Scale = new Vector2(.1f, .1f);
        light.Color = new Color(1f, .7f, .7f, .2f);
        AddChild(light);

        VisibilityNotifier2D onScreenNotifier = new VisibilityNotifier2D();
        onScreenNotifier.Connect("screen_exited", this, "queue_free");
        AddChild(onScreenNotifier);

        Position = Position + Direction * 10f;

        // Sprite
        sprite.Texture = spriteImage;
        AddChild(sprite);

        // Collider
        hitArea.CollisionLayer = 0;
        hitArea.SetCollisionMaskBit(0, false);
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
        explosionEmitter.ProcessMaterial = explosionMaterial;
        explosionEmitter.Emitting = false;
        explosionEmitter.Amount = 150;
        explosionEmitter.Lifetime = 4;
        explosionEmitter.OneShot = true;
        explosionEmitter.SpeedScale = 2;
        explosionEmitter.Explosiveness = 1;
        explosionEmitter.ZIndex = -100;
        AddChild(explosionEmitter);
        
        smokeTrailEmitter.Amount = 200;
        smokeTrailEmitter.Preprocess = 0.5f;
        smokeTrailEmitter.ProcessMaterial = smokeTrailMaterial;
        AddChild(smokeTrailEmitter);

        fireTrailEmitter.Amount = 100;
        fireTrailEmitter.Preprocess = 0.5f;
        fireTrailEmitter.SpeedScale = 1.5f;
        fireTrailEmitter.Explosiveness = .1f;
        fireTrailEmitter.Randomness = 1;
        fireTrailEmitter.ProcessMaterial = fireTrailMaterial;
        fireTrailEmitter.Transform = new Godot.Transform2D(0f, new Vector2(-1, 0f));
        AddChild(fireTrailEmitter);

        // Timers
        Color targetColor =  new Color(light.Color.r, light.Color.g, light.Color.b, light.Color.a*2);
        Color targetColorNone = new Color(light.Color.r, light.Color.g, light.Color.b, 0);
        Vector2 targetScale = light.Scale * 3 * Mathf.Clamp((float)new Random().NextDouble(), .5f, 1f);
        onHitTween.InterpolateProperty(light, "color", light.Color, targetColor, .2f, easeType: Tween.EaseType.Out);
        onHitTween.InterpolateProperty(light, "color", targetColor, targetColorNone, .1f, easeType: Tween.EaseType.Out, delay: .2f);
        onHitTween.InterpolateProperty(light, "scale", light.Scale, targetScale, .1f);
        onHitTween.InterpolateProperty(explosionMaterial, "gravity", explosionMaterial.Gravity, default(Vector3), 0.1f, delay: .2f);
        onHitTween.InterpolateCallback(this, 1f,"queue_free");
        AddChild(onHitTween);

        lifetime.Connect("timeout", this, nameof(Explode));
    }

    public override void _Ready(){
        base._Ready();
        Rotate(GetAngleTo(Position + Direction));
    }

    public override void _PhysicsProcess(float delta){
        if (!exploded){
            base._PhysicsProcess(delta);
            Velocity = AccelerateLogarithmic(Velocity);
            Translate(Velocity * delta);
            ZIndex = Mathf.FloorToInt(Position.y);
        }
    }

    public virtual void _OnHurtAreaEnter(Area2D _ = null){
        if (!exploded) Explode();
    }

    public virtual void Explode(){
        lifetime.Disconnect("timeout", this, nameof(Explode));
        exploded = true;
        onHitTween.Start();
        fireTrailEmitter.Emitting = false;
        smokeTrailEmitter.Emitting = false;
        smokeTrailEmitter.Scale = default(Vector2);
        explosionMaterial.Gravity = new Vector3(Velocity.Length()/2, 0f, 0f);
        explosionEmitter.Emitting = true;
        sprite.Visible = false;
        hitArea.QueueFree();
        DamageNearby();
    }

    public virtual void DamageNearby(){
        foreach (Enemy target in GetTree().GetNodesInGroup("Enemies")){
            if (Position.DistanceTo(target.Position) < effectRadius)
                target.Damage(damage, GlobalPosition.DirectionTo(target.GlobalPosition) * knockbackStrength);
        }
    }
}
