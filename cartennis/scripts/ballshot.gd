
extends RigidBody3D
class_name BallShot

@export_group("Base Shot Settings")
@export var low_ball_height: float = 0.15
@export var high_ball_height: float = 0.8
@export var maximum_shot_distance: float = 2.0

@export_group("Vertical Arc Heights")
@export var maximum_smash_drop: float = 0.5
@export var maximum_lob_height: float = 1.5

@export_group("Shot Behaviour")
@export_range(0.0, 1.0) var height_arc_weight: float = 0.65
@export_range(0.0, 1.0) var distance_arc_weight: float = 0.35

@export_group("Height Reference")
@export var table_height: float = 0.0

@export_group("Gravity Bend")
@export var gravity_bend: float = 0.0

var ball_mirror_active: bool = false
var remaining_boomerang_returns: int = 0
var is_outgoing_player_shot: bool = false


func set_ball_mirror_active(active: bool):
	ball_mirror_active = active


func try_calculate_preview(
	start: Vector3,
	target: Vector3,
	speed_multiplier: float = 1.0
) -> Dictionary:

	if speed_multiplier <= 0.0:
		return {}

	var result = _try_calculate_shot(start, target)

	if result.is_empty():
		return {}

	var flight_time: float = result.flight_time / speed_multiplier

	if flight_time <= 0.001:
		return {}

	var gravity := Vector3.DOWN * _get_gravity()

	var velocity := (
		(target - start) / flight_time
		- 0.5 * gravity * flight_time
	)

	return {
		"velocity": velocity,
		"flight_time": flight_time
	}


func launch_shot(target: Vector3) -> bool:
	return launch_velocity_toward(target)


func replay_shot(target: Vector3) -> bool:
	return launch_velocity_toward(target)


func launch_velocity_toward(target: Vector3) -> bool:
	var result = _try_calculate_shot(global_position, target)

	if result.is_empty():
		push_warning("Could not calculate a valid shot.")
		is_outgoing_player_shot = false
		return false

	var velocity: Vector3 = result.velocity

	if ball_mirror_active:
		velocity.x = -velocity.x

	linear_velocity = velocity
	angular_velocity = Vector3.ZERO

	is_outgoing_player_shot = true
	return true


func _physics_process(_delta):
	if absf(gravity_bend) < 0.001:
		return

	if global_position.y <= table_height + 0.05:
		return

	apply_central_force(Vector3.RIGHT * gravity_bend * mass)


func _try_calculate_shot(start: Vector3, target: Vector3) -> Dictionary:
	var gravity := _get_gravity()

	if gravity <= 0.001:
		return {}

	var distance := Vector2(
		start.x - target.x,
		start.z - target.z
	).length()

	var ball_height := start.y - table_height

	var height_factor := inverse_lerp(
		low_ball_height,
		high_ball_height,
		ball_height
	)

	var distance_factor := inverse_lerp(
		0.25,
		maximum_shot_distance,
		distance
	)

	var arc_factor := (
		(1.0 - height_factor)
		* (height_arc_weight + distance_factor * distance_arc_weight)
	)

	arc_factor = clampf(arc_factor, 0.0, 1.0)

	var desired_peak_height := lerpf(
		-maximum_smash_drop,
		maximum_lob_height,
		arc_factor
	)

	var vertical_velocity: float

	if desired_peak_height > 0.0:
		vertical_velocity = sqrt(2.0 * gravity * desired_peak_height)
	else:
		vertical_velocity = -sqrt(2.0 * gravity * -desired_peak_height)

	var vertical_difference := target.y - start.y

	var discriminant := (
		vertical_velocity * vertical_velocity
		- 2.0 * gravity * vertical_difference
	)

	if discriminant < 0.0:
		return {}

	var flight_time := (
		vertical_velocity + sqrt(discriminant)
	) / gravity

	if flight_time <= 0.001:
		return {}

	var horizontal_difference := Vector3(
		target.x - start.x,
		0.0,
		target.z - start.z
	)

	var velocity := (
		horizontal_difference / flight_time
		+ Vector3.UP * vertical_velocity
	)

	return {
		"velocity": velocity,
		"flight_time": flight_time
	}


func evaluate_trajectory(
	start: Vector3,
	initial_velocity: Vector3,
	elapsed_time: float
) -> Vector3:

	var gravity := Vector3.DOWN * _get_gravity()

	return (
		start
		+ initial_velocity * elapsed_time
		+ 0.5 * gravity * elapsed_time * elapsed_time
	)


func set_gravity_bend(strength: float):
	gravity_bend = strength


func _get_gravity() -> float:
	return float(ProjectSettings.get_setting("physics/3d/default_gravity"))
