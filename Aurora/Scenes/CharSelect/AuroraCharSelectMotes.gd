extends CPUParticles2D

# 环境紫色能量微尘：缓慢上升飘动，additive 叠加，营造"活着的场景"氛围。
# 参数用脚本设，避免 .tscn 里枚举/属性名手误。位置/范围以 1920x1080 设计，进 Godot 可微调。
func _ready() -> void:
	texture = load("res://Aurora/Images/CharSelect/fx_mote.png")
	amount = 90
	lifetime = 8.0
	preprocess = 6.0
	randomness = 1.0
	emission_shape = CPUParticles2D.EMISSION_SHAPE_RECTANGLE
	emission_rect_extents = Vector2(1000, 600)
	direction = Vector2(0, -1)
	spread = 25.0
	gravity = Vector2(0, -6)
	initial_velocity_min = 4.0
	initial_velocity_max = 18.0
	scale_amount_min = 0.2
	scale_amount_max = 1.1
	color = Color(0.74, 0.54, 1.0, 0.5)
	# 生命周期内渐隐，尾部更自然
	var grad := Gradient.new()
	grad.set_color(0, Color(0.85, 0.65, 1.0, 0.0))
	grad.add_point(0.25, Color(0.8, 0.6, 1.0, 0.55))
	grad.set_color(1, Color(0.7, 0.5, 1.0, 0.0))
	color_ramp = grad
	emitting = true
