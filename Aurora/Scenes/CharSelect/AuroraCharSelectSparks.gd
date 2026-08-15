extends CPUParticles2D

# 剑/角色周围更亮更细的能量火花，上升略快，additive；与底层微尘拉开层次。
func _ready() -> void:
	texture = load("res://Aurora/Images/CharSelect/fx_glow.png")
	amount = 40
	lifetime = 4.5
	preprocess = 3.0
	randomness = 1.0
	emission_shape = CPUParticles2D.EMISSION_SHAPE_RECTANGLE
	emission_rect_extents = Vector2(360, 420)
	direction = Vector2(0, -1)
	spread = 18.0
	gravity = Vector2(0, -14)
	initial_velocity_min = 10.0
	initial_velocity_max = 34.0
	scale_amount_min = 0.05
	scale_amount_max = 0.18
	var grad := Gradient.new()
	grad.set_color(0, Color(0.95, 0.8, 1.0, 0.0))
	grad.add_point(0.3, Color(0.85, 0.65, 1.0, 0.85))
	grad.set_color(1, Color(0.75, 0.5, 1.0, 0.0))
	color_ramp = grad
	emitting = true
