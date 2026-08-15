extends TextureRect

# 选人立绘极轻微呼吸：整图缓慢上下浮动（不放大缩小，避免机甲/剑变形），正弦缓入缓出循环。
# 单张图不切件，脸/剑/机甲一个像素没变形，只做"活着"的上下起伏呼吸感。
const AMPLITUDE := 9.0   # 上下浮动幅度（像素），可微调
const PERIOD := 2.4      # 单程时长（秒）

func _ready() -> void:
	await get_tree().process_frame
	var base_y := position.y
	var t := create_tween().set_loops()
	t.tween_property(self, "position:y", base_y - AMPLITUDE, PERIOD) \
		.set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
	t.tween_property(self, "position:y", base_y, PERIOD) \
		.set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
