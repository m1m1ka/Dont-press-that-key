# AI Coding Guidelines

## Game Rules

The game is a two-sided shotgun duel between the player and an enemy.

- The player and the enemy share one shotgun loaded with a mix of live shells and blank shells.
- Both sides have their own health value.
- A side dies when its health reaches zero.
- On a turn, the current actor may shoot either themselves or the opponent.
- If the current actor shoots themselves, the next turn still belongs to the same actor.
- If the current actor shoots the opponent, the next turn switches to the other actor.
- Live shells should damage the shot target. Blank shells should not damage the shot target.
- Keep these turn rules consistent for both player and enemy actions.

## Animation Rule

All gameplay movement, rotation, and scale animations must use DOTween.

- Use `DOMove`, `DOLocalMove`, `DORotate`, `DORotateQuaternion`, `DOLocalRotate`, `DOScale`, or `Sequence` for Transform animation.
- Do not hand-roll gameplay animation with `Vector3.Lerp`, `Quaternion.Slerp`, coroutine loops, or `Update`/`LateUpdate` interpolation.
- Reuse serialized `AnimationCurve` fields through DOTween `SetEase(curve)` when designers need adjustable timing.
- Keep a `Tween` or `Sequence` reference when the animation can be interrupted, and kill it before starting a replacement.
- Kill owned tweens in `OnDisable` or `OnDestroy`.

## Rigidbody Rule

When a Rigidbody object is moved by DOTween, physics must not fight the tween.

- AmmoBox is not physics-driven. Spawn it at the configured spawn position, disable Rigidbody gravity/physics ownership, then move it to the configured landing position with Transform DOTween.
- Prefer DOTween Rigidbody shortcuts such as `Rigidbody.DOMove` and `Rigidbody.DORotate` for Rigidbody-owned movement.
- Use `SetUpdate(UpdateType.Fixed)` for Rigidbody movement tweens.
- Before tweening a Rigidbody object, clear `velocity` and `angularVelocity`.
- Set the Rigidbody to `isKinematic = true` while script animation owns the object.
- Use `RigidbodyInterpolation.Interpolate` for visible objects that are moved through physics tweens.

## Interaction Rule

Cross-object gameplay communication should go through `GameEventBus` unless the scripts already have a clear owner-child relationship.
