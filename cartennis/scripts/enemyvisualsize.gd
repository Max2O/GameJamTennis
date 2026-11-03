
extends Sprite3D

@export var reference_texture: Texture2D

# The pixel size you manually configured for the idle pose.
@export var reference_pixel_size: float = 0.01

var target_world_height: float


func _ready():
	if reference_texture == null:
		push_warning("No reference texture assigned!")
		return

	target_world_height = (
		reference_texture.get_height()
		* reference_pixel_size
	)

	# Apply the correct size to the current texture.
	adjust_sprite_size(texture)


func adjust_sprite_size(new_texture: Texture2D):
	if new_texture == null:
		return

	pixel_size = (
		target_world_height
		/ new_texture.get_height()
	)


func change_pose(new_texture: Texture2D):
	texture = new_texture
	adjust_sprite_size(new_texture)
