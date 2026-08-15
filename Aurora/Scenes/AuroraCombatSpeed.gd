extends SpineSprite

# 按动画名单独调时间倍率：attack 提速（出手更快）、hurt 减速（受击太快看不清）。
# idle/die 不受影响。全程防御式：任何一步取不到就静默跳过，绝不影响 spine 正常渲染/游戏驱动。
const SPEED := {
	"attack": 1.6,
	"hurt": 0.5,
}

func _ready() -> void:
	if has_signal("animation_started") and not animation_started.is_connected(_on_anim_started):
		animation_started.connect(_on_anim_started)

# spine-godot 的 animation_started 信号回调是 3 个参数：(sprite, animation_state, track_entry)。
# 之前只写 2 个 → 每次触发都报 "expected 2, called with 3" → 调速完全没生效。
func _on_anim_started(_sprite, _state, entry) -> void:
	if entry == null:
		return
	var anim = entry.get_animation()
	if anim == null:
		return
	var n := str(anim.get_name())
	if SPEED.has(n):
		entry.set_time_scale(SPEED[n])
