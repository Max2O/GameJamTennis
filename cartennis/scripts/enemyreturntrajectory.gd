
extends Node

enum FlightLeg {
	TO_TABLE,
	TO_PLAYER,
	COMPLETE
}

var ball: RigidBody3D
var player_shot: BallShot

var table: StaticBody3D
var player_hitbox: Area3D
var net: CollisionObject3D

var landing_point: Vector3
var player_target: Vector3

var minimum_arc_height: float = 0.1
var ball_radius: float = 0.1

var current_leg: FlightLeg = FlightLeg.COMPLETE
var is_boomerang_return: bool = false


func _ready():
	ball = get_parent() as RigidBody3D

	if ball == null:
		push_error("EnemyReturnTrajectory must be a child of a RigidBody3D.")
		return

	player_shot = ball as BallShot

	if player_shot == null:
		push_error("The Ball must have the PlayerShot script.")
		return

	ball.body_entered.connect(_on_ball_body_entered)


func begin(
	table_body: StaticBody3D,
	player: Area3D,
	net_body: CollisionObject3D,
	destination: Vector3,
	target: Vector3,
	speed_multiplier: float,
	arc_height: float,
	radius: float
):
	table = table_body
	player_hitbox = player
	net = net_body

	landing_point = destination
	player_target = target

	minimum_arc_height = arc_height
	ball_radius = radius

	current_leg = FlightLeg.TO_TABLE
	is_boomerang_return = false

	ball.linear_velocity = Vector3.ZERO
	ball.angular_velocity = Vector3.ZERO

	if not try_launch(landing_point, speed_multiplier):
		current_leg = FlightLeg.COMPLETE


func try_launch(destination: Vector3, speed_multiplier: float) -> bool:
	var result = player_shot.try_calculate_preview(
		ball.global_position,
		destination,
		speed_multiplier
	)

	if result.is_empty():
		push_warning("Could not calculate enemy shot trajectory.")
		return false

	var velocity: Vector3 = result.velocity
	var flight_time: float = result.flight_time

	# Match the player's sideways gravity compensation.
	velocity.x -= 0.5 * player_shot.gravity_bend * flight_time

	ball.linear_velocity = velocity
	ball.angular_velocity = Vector3.ZERO

	return true


func try_launch_at_speed(destination: Vector3, speed: float) -> bool:
	if speed <= 0.001:
		return false

	var start := ball.global_position
	var gravity := Vector3.DOWN * _get_gravity()

	var horizontal_distance := Vector2(
		destination.x - start.x,
		destination.z - start.z
	).length()

	var duration := horizontal_distance / speed

	duration = maxf(
		duration,
		sqrt(8.0 * minimum_arc_height / _get_gravity())
	)

	duration = maxf(
		duration,
		get_net_clearance_duration(start, destination)
	)

	duration = maxf(duration, 0.02)

	var velocity := (
		(destination - start) / duration
		- 0.5 * gravity * duration
	)

	velocity.x -= 0.5 * player_shot.gravity_bend * duration

	ball.linear_velocity = velocity
	ball.angular_velocity = Vector3.ZERO

	return true


func _on_ball_body_entered(body: Node3D):
	if current_leg != FlightLeg.TO_TABLE:
		return

	if body != table:
		return

	current_leg = FlightLeg.TO_PLAYER

	var rebound_speed := calculate_rebound_speed()

	if rebound_speed <= 0.001:
		current_leg = FlightLeg.COMPLETE
		return

	if not try_launch_at_speed(player_target, rebound_speed):
		current_leg = FlightLeg.COMPLETE


func calculate_rebound_speed() -> float:
	# Approximation using the ball's current velocity and
	# its PhysicsMaterial bounce/friction values.
	var material := ball.physics_material_override

	var restitution := 0.0
	var friction := 0.6

	if material != null:
		restitution = material.bounce
		friction = material.friction

	var incoming_velocity := ball.linear_velocity
	var normal := Vector3.UP

	var incoming_normal_speed := -incoming_velocity.dot(normal)

	if incoming_normal_speed <= 0.001:
		return 0.0

	var tangent_velocity := incoming_velocity.slide(normal)

	var friction_impulse := (
		friction * (1.0 + restitution) * incoming_normal_speed
	)

	var tangent_speed := maxf(
		0.0,
		tangent_velocity.length() - friction_impulse
	)

	var normal_speed := restitution * incoming_normal_speed

	return sqrt(
		normal_speed * normal_speed
		+ tangent_speed * tangent_speed
	)


func get_net_clearance_duration(start: Vector3, destination: Vector3) -> float:
	if net == null:
		return 0.0

	var net_shape := _get_net_shape()
	if net_shape == null:
		return 0.0

	var net_bounds := net_shape.shape.get_debug_mesh()
	if net_bounds == null:
		return 0.0

	# We'll replace this with an accurate net-clearance calculation
	# once the net's collision dimensions are connected.
	return 0.0


func _get_net_shape() -> CollisionShape3D:
	if net == null:
		return null

	for child in net.get_children():
		if child is CollisionShape3D:
			return child

	return null


func _get_gravity() -> float:
	return float(ProjectSettings.get_setting("physics/3d/default_gravity"))


func reset_trajectory():
	current_leg = FlightLeg.COMPLETE
	ball.linear_velocity = Vector3.ZERO
	ball.angular_velocity = Vector3.ZERO


func is_heading_to_player() -> bool:
	return current_leg == FlightLeg.TO_PLAYER
