extends Sprite2D

# 她身后的辉光缓慢脉冲(呼吸式明暗),additive 叠加。
func _ready() -> void:
	modulate.a = 0.55
	var t := create_tween().set_loops()
	t.tween_property(self, "modulate:a", 0.9, 2.6) \
		.set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
	t.tween_property(self, "modulate:a", 0.55, 2.6) \
		.set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
