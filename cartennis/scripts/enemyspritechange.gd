
extends Node3D

@export var ball: RigidBody3D
@export var sprite: Sprite3D

@export var idle_texture: Texture2D
@export var ready_texture: Texture2D
@export var hitting_texture: Texture2D


var is_attacking := false
@export var strike_distance: float = 2.0

enum EnemyState {
	IDLE,
	READY,
	HITTING
}

var current_state: EnemyState = EnemyState.IDLE


func start_attack():
	if is_attacking:
		return

	is_attacking = true
	sprite.change_pose(hitting_texture)

	await get_tree().create_timer(0.3).timeout

	is_attacking = false
	update_appearance()


func _physics_process(_delta):
	if ball == null or sprite == null:
		return

	global_position.x = ball.global_position.x

	update_appearance()


func update_appearance():
	if is_attacking:
		return

	var ball_z := ball.global_position.z

	if ball_z > 0:
		change_state(EnemyState.IDLE)
	else:
		change_state(EnemyState.READY)


func change_state(new_state: EnemyState):
	if current_state == new_state:
		return

	current_state = new_state

	match current_state:
		EnemyState.IDLE:
			sprite.change_pose(idle_texture)

		EnemyState.READY:
			sprite.change_pose(ready_texture)
