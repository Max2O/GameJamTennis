
extends Area3D

@export var ball: RigidBody3D
@export var player_hitbox: Area3D
@export var player_side_area: Area3D
@export var table: StaticBody3D
@export var net: CollisionObject3D

@export_group("Enemy Attack Settings")
@export var bounceback_percent: float = 100.0
@export var attack_speed_multiplier: float = 1.0
@export var minimum_arc_height: float = 0.1

@export_group("Enemy Appearance")
@export var enemy_sprite: Sprite3D
@export var attacking_texture: Texture2D
@export var attack_animation_duration: float = 0.3

var has_returned_ball: bool = false


func _ready():
	body_entered.connect(_on_body_entered)
	body_exited.connect(_on_body_exited)


func _on_body_entered(body: Node3D):
	if has_returned_ball or body != ball:
		return

	if ball == null or player_hitbox == null:
		return

	if bounceback_percent <= 0.0:
		return

	if attack_speed_multiplier <= 0.0:
		return

	if minimum_arc_height < 0.0:
		return

	var player_target := get_random_point_inside(player_hitbox)

	var landing_point := get_random_landing_point(
		ball.global_position,
		player_target
	)

	if landing_point == Vector3.ZERO:
		push_warning("Could not find a valid enemy landing point.")
		return

	var trajectory := ball.get_node_or_null("EnemyReturnTrajectory")

	if trajectory == null:
		push_error("Ball is missing EnemyReturnTrajectory.")
		return

	var ball_shape := ball.get_node("CollisionShape3D") as CollisionShape3D
	var radius := 0.1

	if ball_shape != null and ball_shape.shape is SphereShape3D:
		radius = ball_shape.shape.radius

	has_returned_ball = true

	trajectory.begin(
		table,
		player_hitbox,
		net,
		landing_point,
		player_target,
		attack_speed_multiplier * bounceback_percent / 100.0,
		minimum_arc_height,
		radius
	)

	get_parent().start_attack()


func _on_body_exited(body: Node3D):
	if body == ball:
		has_returned_ball = false


func get_random_point_inside(area: Area3D) -> Vector3:
	var collision_shape := _find_collision_shape(area)

	if collision_shape == null:
		return area.global_position

	if collision_shape.shape is BoxShape3D:
		var box := collision_shape.shape as BoxShape3D
		var half_size := box.size * 0.5

		var local_point := Vector3(
			randf_range(-half_size.x, half_size.x),
			randf_range(-half_size.y, half_size.y),
			randf_range(-half_size.z, half_size.z)
		)

		return collision_shape.to_global(local_point)

	return area.global_position


func get_random_landing_point(start: Vector3, target: Vector3) -> Vector3:
	if player_side_area == null or table == null:
		return Vector3.ZERO

	var area_shape := _find_collision_shape(player_side_area)

	if area_shape == null or not area_shape.shape is BoxShape3D:
		return Vector3.ZERO

	var box := area_shape.shape as BoxShape3D
	var half_size := box.size * 0.5

	# Convert the ball and player target into the area's local coordinates.
	var local_start := area_shape.to_local(start)
	var local_target := area_shape.to_local(target)

	# The line between these points defines the vertical plane
	# from which we select a random landing position.
	var direction := Vector2(
		local_target.x - local_start.x,
		local_target.z - local_start.z
	)

	var plane_normal := Vector2(direction.y, -direction.x).normalized()

	if direction.length_squared() < 0.000001:
		plane_normal = Vector2.RIGHT

	var plane_offset := plane_normal.dot(
		Vector2(local_start.x, local_start.z)
	)

	# Intersect the plane with the rectangular area.
	var intersections: Array[Vector2] = []

	var corners := [
		Vector2(-half_size.x, -half_size.z),
		Vector2(half_size.x, -half_size.z),
		Vector2(half_size.x, half_size.z),
		Vector2(-half_size.x, half_size.z)
	]

	for i in range(4):
		var a: Vector2 = corners[i]
		var b: Vector2 = corners[(i + 1) % 4]

		var da := plane_normal.dot(a) - plane_offset
		var db := plane_normal.dot(b) - plane_offset

		if absf(da) < 0.0001:
			_add_intersection(intersections, a)

		if absf(db) < 0.0001:
			_add_intersection(intersections, b)

		if (da < 0.0) != (db < 0.0):
			var crossing := a.lerp(b, da / (da - db))
			_add_intersection(intersections, crossing)

	if intersections.size() < 2:
		return Vector3.ZERO

	var longest_start := intersections[0]
	var longest_end := intersections[1]
	var longest_distance := 0.0

	for i in range(intersections.size()):
		for j in range(i + 1, intersections.size()):
			var distance := intersections[i].distance_to(intersections[j])

			if distance > longest_distance:
				longest_distance = distance
				longest_start = intersections[i]
				longest_end = intersections[j]

	if longest_distance <= 0.0001:
		return Vector3.ZERO

	var direct_space := get_world_3d().direct_space_state

	for attempt in range(32):
		var sample := longest_start.lerp(longest_end, randf())

		var local_origin := Vector3(
			sample.x,
			half_size.y + 2.0,
			sample.y
		)

		var origin := area_shape.to_global(local_origin)
		var endpoint := origin + Vector3.DOWN * 10.0

		var query := PhysicsRayQueryParameters3D.create(origin, endpoint)
		query.exclude = [ball.get_rid()]
		query.collision_mask = table.collision_layer

		var hit := direct_space.intersect_ray(query)

		if not hit.is_empty():
			return hit.position + hit.normal * 0.1

	return Vector3.ZERO


func _add_intersection(points: Array[Vector2], point: Vector2):
	for existing in points:
		if existing.distance_to(point) < 0.0001:
			return

	points.append(point)


func _find_collision_shape(node: Node) -> CollisionShape3D:
	for child in node.get_children():
		if child is CollisionShape3D:
			return child

	return null
