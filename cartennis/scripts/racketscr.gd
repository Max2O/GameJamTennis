
extends Area3D

@export_group("References")
@export var camera: Camera3D

@export_group("Aiming")
@export_flags_3d_physics var aim_collision_mask: int = 1

@export_group("Down Hit")
@export var down_hit_bonus: float = 5.0
@export var racket_speed_multiplier: float = 1.0

var racket_velocity := Vector3.ZERO
var previous_position := Vector3.ZERO


func _ready():
	previous_position = global_position
	body_entered.connect(_on_body_entered)


func _physics_process(delta):
	if camera == null:
		return

	move_racket_to_mouse()
	calculate_racket_velocity(delta)


func move_racket_to_mouse():
	var mouse_position = get_viewport().get_mouse_position()

	var ray_origin = camera.project_ray_origin(mouse_position)
	var ray_direction = camera.project_ray_normal(mouse_position)

	var ray_end = ray_origin + ray_direction * 1000.0

	var query = PhysicsRayQueryParameters3D.create(
		ray_origin,
		ray_end,
		aim_collision_mask
	)

	var hit = get_world_3d().direct_space_state.intersect_ray(query)

	if hit:
		global_position = hit.position


func calculate_racket_velocity(delta):
	if delta <= 0.0:
		return

	racket_velocity = (global_position - previous_position) / delta
	previous_position = global_position


func _on_body_entered(body):
	if not body is BallShot:
		return

	hit_ball(body)


func hit_ball(ball: BallShot):
	var incoming_forward_speed = ball.linear_velocity.z

	# Reverse and halve the ball's forward speed.
	var returned_forward_speed = -incoming_forward_speed * 0.5

	# Replace the ball's sideways and vertical velocity
	# with the racket's movement velocity.
	var returned_velocity = Vector3(
		racket_velocity.x,
		racket_velocity.y,
		returned_forward_speed
	)

	# Optional stronger hit while the down-hit button is held.
	if Input.is_action_pressed("racket_boost"):
		var racket_speed = Vector2(
			racket_velocity.x,
			racket_velocity.y
		).length()

		var boost = (
			down_hit_bonus
			+ racket_speed * racket_speed_multiplier
		)

		# The enemy is in the negative-Z direction.
		returned_velocity.z -= boost

	ball.linear_velocity = returned_velocity
	ball.angular_velocity = Vector3.ZERO
	ball.is_outgoing_player_shot = true
