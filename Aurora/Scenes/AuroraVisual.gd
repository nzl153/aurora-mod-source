extends SpineSprite

# 立绘一进场景就循环播待机，方便预览。
func _ready() -> void:
	var st := get_animation_state()
	if st != null:
		st.set_animation("idle_loop", true, 0)
