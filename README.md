# OneWeekGameJam_Ten
[Unity1週間ゲームジャム : お題「10」](https://unityroom.com/unity1weeks/11) 投稿作品ソース

「Unity1週間ゲームジャム : お題「10」」にて投稿した作品のソース一式です。  
PureECS & MonoBehaviourの相互連携を踏まえた上で設計を試みたゲームとなります。  
→ 例えば今回の例で言えばプレイヤーキャラはGameObject(SpriteRenderer)で実装し、それ以外の弾や敵はECSで制御するなど。

※[投稿作品はこちらからプレイ可能](https://unityroom.com/games/barrage_10)

こちらはコード公開版となるので、一部のリソース等についてはライセンスの理由で取り除いており、大まかに纏めると以下の変更点が入っております。  
それ以外については投稿作品と同一です。

- エフェクトなし
- Textureを仮素材に差し替え(※本家は「いらすとや」さんのTextureを使用)
- Ranking & Tweetなし
- 一部Fade/Camera Shakeと言った演出をカット(DOTweenを使用しているため)

※短期間で作った作品のために設計的に最適解ではない可能性もある上で、デバッグしきれていない部分もあるかもしれませんが...そこらを踏まえた上でご参考にして頂けると幸いです。  

- 追記 : 技術解説記事も書きました。
  - [【Unity】ECS + GameObject/MonoBehaviourの連携を踏まえてゲームを作ってみたので技術周りについて解説](https://qiita.com/mao_/items/332aa226d7eb35112956)


### versions.

- **Unity**
  - 2018.3.0f2
- **dependencies**
  - "com.unity.entities": "0.0.12-preview.21"
  - "com.unity.postprocessing": "2.1.2"



# ▽ License

- [neuecc/UniRx - LICENSE](https://github.com/neuecc/UniRx/blob/master/LICENSE)
- [SE: 魔王魂](https://maoudamashii.jokersounds.com/)
