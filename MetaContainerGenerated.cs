using System;
using System.Collections.Generic;
using System.Reflection;

using Thrift.Collections;
using Thrift.Transport;
using Thrift.Protocol;
using System.Diagnostics;

namespace gen
{
	public partial class MetaContainerGroup
	{
		//최초 생성시, 각 카테고리 에 맞는 id의 더미 데이터를 입력해주자. 
		public TBase allocMetaContainer()
		{
			//string container_name = m_category + "_meta.container, csGenThrift";
			//Type meta_container_type = Type.GetType(container_name);
			//return (TBase)Activator.CreateInstance(meta_container_type);

			// add dummy, 0번 id는 내부에서 무시 하도록 처리하자.
			switch (m_category)
			{
				case "club":
					{
						var container = new club_meta.container();
						container.Infos = new Dictionary<int, club_meta.data>();
						var data = new club_meta.data();
						container.Infos.Add(0, data);
						return container;
					}

				case "ball":
					{
						var container = new ball_meta.container();
						container.Infos = new Dictionary<int, ball_meta.data>();
						var data = new ball_meta.data();
						container.Infos.Add(0, data);
						return container;
					}
				case "physics":
					{
						var container = new physics_meta.container();
						container.Infos = new Dictionary<int, physics_meta.data>();
						var data = new physics_meta.data();
						container.Infos.Add(0, data);
						return container;
					}
				case "course":
					{
						var container = new course_meta.container();
						container.Infos = new Dictionary<int, course_meta.data>();
						var data = new course_meta.data();
						container.Infos.Add(0, data);
						return container;
					}

				default:
					break;
			}

			return null;
		}

		//auto generated.
		public void tryMergeContainers()
		{
			if (true == m_merged)
			{
				return;
			}
			m_merged = true;

			foreach (var container in m_hash.Values)
			{
				switch (m_category)
				{
					case "club":
						merge_club_meta(container);
						break;
					case "ball":
						merge_ball_meta(container);
						break;
					case "physics":
						merge_physics_meta(container);
						break;
					case "course":
						merge_course_meta(container);
						break;
					default:
						break;
				}
			}
		}


		//auto generated.
		void merge_club_meta(TBase container)
		{
			var meta_container = (club_meta.container)container;
			foreach (var pair in meta_container.Infos)
			{
				infos<club_meta.data>().Add(pair.Key, pair.Value);
			}
		}

		void merge_ball_meta(TBase container)
		{
			var meta_container = (ball_meta.container)container;

			foreach (var pair in meta_container.Infos)
			{
				infos<ball_meta.data>().Add(pair.Key, pair.Value);
			}
		}

		void merge_physics_meta(TBase container)
		{
			var meta_container = (physics_meta.container)container;
			foreach (var pair in meta_container.Infos)
			{
				infos<physics_meta.data>().Add(pair.Key, pair.Value);
			}
		}

		void merge_course_meta(TBase container)
		{
			var meta_container = (course_meta.container)container;
			foreach (var pair in meta_container.Infos)
			{
				infos<course_meta.data>().Add(pair.Key, pair.Value);
			}
		}
	}
}
