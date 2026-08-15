extends SpineSprite

const ANIMATION := "idle"
const FLOAT_AMPLITUDE := 9.0
const FLOAT_PERIOD := 2.4

func _ready() -> void:
	var state := get_animation_state()
	if state != null:
		state.set_animation(ANIMATION, true, 0)

	await get_tree().process_frame
	var base_y := position.y
	var tween := create_tween().set_loops()
	tween.tween_property(self, "position:y", base_y - FLOAT_AMPLITUDE, FLOAT_PERIOD) \
		.set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
	tween.tween_property(self, "position:y", base_y, FLOAT_PERIOD) \
		.set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
