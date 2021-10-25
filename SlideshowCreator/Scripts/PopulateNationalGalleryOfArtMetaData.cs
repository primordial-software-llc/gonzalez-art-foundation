using IndexBackend.Sources.NationalGalleryOfArt;
using NUnit.Framework;

namespace SlideshowCreator.Scripts
{
    class PopulateNationalGalleryOfArtMetaData
    {
        [Test]
        public void Asset_Details_From_Html_To_New_Model()
        {
            #region Static HTML Sample
            var html = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"">
<html xmlns=""http://www.w3.org/1999/xhtml"">
  <head>
    <meta http-equiv=""Content-Type"" content=""text/html; charset=UTF-8"" />
    <base href=""https://images.nga.gov/"" />
    <title></title>
    <link rel=""stylesheet"" type=""text/css"" media=""screen"" href=""?service=stylesheet&amp;action=get_style&amp;layout_style&amp;main_style&amp;fader_box_style"" />
    <style type=""text/css"" media=""print"">
		div#top{
			display:none;
		}
		div#left{
			display:none;
		}
		div#Footer{
		    display:none;
		}
		div#pagenav {
		    display:none;
		}
		div#mainNav {
		    display:none !important;
		}
		div#itemPopupWrapper div#pagenav {
			margin-top: 8px !important;
		}
		#submitSimilars {
			margin-right: 5px !important;
		}
		.headerImage {
			padding-left: 0 !important;
			padding-bottom: 40px;
			margin-left: 0 !important;
		}
		p.metadata {
			display: block !important;
		}
	</style>
    <script type=""text/javascript"" src=""?service=javascript&amp;action=get_script&amp;scriptaculous_library&amp;fader_box_library&amp;swf_object_library&amp;mm_library&amp;basket_script&amp;basket_event_script&amp;lightbox_script&amp;lightbox_event_script&amp;user_script&amp;user_event_script&amp;asset_script&amp;asset_event_script&amp;search_script""></script>
    <script type=""text/javascript"">
		var Basket = new Basket();
		var Lightbox = new Lightbox('');
		var ZoomPrice = new ZoomPrice();

		window.onload = function()
		{
			// Basket
			Basket.setLanguage('en');
			Basket.init();
			//

			// Lightbox
			Lightbox.setLanguage('en');
			Lightbox.init();
			//

			
		}

		function mailAsset(asset) {
			var mainWindow = Basket.getMainWindow();

			if(!mainWindow.Lightbox.overlayEditPermission())
			{
				Lightbox.showNoEditPermissionWindow();
				return false;
			}

			Asset.showEmailWindow(asset,
				function() {
					$('assetEmailFormErrors').innerHTML = ""<span></span>"";

					Asset.email(asset, $('assetEmailWindowEmail').value, $('assetEmailWindowSubject').value, $('assetEmailWindowDescription').value, 
						function () {
							alert('Email successfully sent');
							Asset.closeEmailWindow();
						}, 
						function () {
							Asset.showEmailFormErrors('en');
						}
					);
				},
				function()
				{
					Asset.closeEmailWindow();
				}
			);
		}

		$(document).observe(""dom:loaded"",function(){
			// add to front of lightbox
			$$('.iconFrontLightbox').each(function(el){
				el.stopObserving(""click"").observe(""click"",function(){

					
							if(Lightbox.managerDeletePermission()){
								zoomLightboxAction_onClick('46482','addToFront',function(){
									Lightbox.cwAlert(""Added to Lightbox"",""This image has been added to your lightbox."",""OK"");
								});
							}
							else{
								Lightbox.showNoDeletePermissionWindow();   
							}
						
				})
			});

			// add to back of lightbox
			$$('.iconBackLightbox').each(function(el){
				el.stopObserving(""click"").observe(""click"",function(){
					
							if(Lightbox.managerDeletePermission()){
								zoomLightboxAction_onClick('46482','addToBack',function(){
									Lightbox.cwAlert(""Added to Lightbox"",""This image has been added to your lightbox."",""OK"");
								});
							}
							else{
								Lightbox.showNoDeletePermissionWindow();   
							}
						
				});
			});
		});
	</script>
  </head>
  <body class=""popup"">
    <div id=""messageWarningWindow"" class=""msgPopupNew"" style=""display: none""></div>
    <input id=""image_previewEntry"" type=""hidden"" value=""https://images.nga.gov/en/search/do_quick_search.html?launchZoom=46482"" />
    <input id=""basket-selected-id"" type=""hidden"" />
    <div id=""notesWindow"" class=""msgPopupNew"" style=""display: none""></div>
    <div id=""itemPopupWrapper"">
      <div id=""top"">
        <div id=""pagenav"">
          <div class=""paging"" style=""width:200px;""><script>var asset_array = [];asset_array[0] = ""46482"";
</script><a href=""javascript:void(0);"" class=""page""><img src=""images/right_arrow_dimmed-onwhite.png"" alt=""next"" /></a><a href=""javascript:void(0);"" class=""page""><img src=""images/left_arrow_dimmed-onwhite.png"" alt=""previous"" /></a><div class=""pageSelectorMain"">Item 
				
						<input class=""text"" style=""text-align:center;"" id=""PageSelectBox"" value=""1"" onchange="" if(asset_array[this.value - 1] != '') {window.location='?service=asset&amp;action=show_zoom_window_popup&amp;language=en&amp;asset=' +  asset_array[this.value - 1] + '&amp;location=grid&amp;asset_list=46482';}"" /> of <a href=""javascript:void(0)"" style=""margin-right:15px;"" onclick="" if(asset_array[asset_array.length - 1] != '') {window.location='?service=asset&amp;action=show_zoom_window_popup&amp;language=en&amp;asset=' +  asset_array[asset_array.length - 1] + '&amp;location=grid&amp;asset_list=46482';}"">1</a></div></div>
        </div>
        <div id=""pagelogo"">
          <a href=""#"" onclick=""window.opener.focus();"">
            <img class=""blockLeft"" alt=""Capture Web 2.0"" src=""images/logo/nga_logotype.png"" />
          </a>
        </div>
        <div class=""text""></div>
        <div id=""pagelinks"">
          <a href=""javascript:void(0)"" id=""prelink"" class=""active"" onclick=""&#10;&#9;&#9;&#9;&#9;&#9;&#9;$('similars_right').style.display='none';&#10;&#9;&#9;&#9;&#9;&#9;&#9;$('preview_right').style.display='block';&#10;&#9;&#9;&#9;&#9;&#9;&#9;$('prelink').addClassName('active');&#10;&#9;&#9;&#9;&#9;&#9;&#9;$('simlink').removeClassName('active');&#10;&#9;&#9;&#9;&#9;&#9;"">Preview
				</a>
          <a href=""javascript:void(0)"" id=""simlink"" onclick=""$('similars_right').style.display='block';&#10;&#9;&#9;&#9;&#9;&#9;&#9;$('preview_right').style.display='none';&#10;&#9;&#9;&#9;&#9;&#9;&#9;$('prelink').removeClassName('active');&#10;&#9;&#9;&#9;&#9;&#9;&#9;$('simlink').addClassName('active');&#10;&#9;&#9;&#9;&#9;&#9;"">Look for Related Images</a>
        </div>
      </div>
      <div id=""main"" class=""clearfix"">
        <div id=""left"">
          <div id=""actions"">
            <ul>
              <li>
                <a href=""javascript:void(0)"" class=""iconPreviewOnly"" onclick=""$('imageimg').ondblclick();return false;"">Preview (Image Only)</a>
              </li>
              <li class=""addToLightbox"">
                <a href=""javascript:void(0)"" class=""iconFrontLightbox"">Add to front of lightbox</a>
              </li>
              <li class=""addToLightbox"">
                <a href=""javascript:void(0)"" class=""iconBackLightbox"">Add to back of lightbox</a>
              </li>
              <li class=""removeFromLightbox hidden"">
                <a href=""javascript:void(0)"" class=""iconLightboxRemove"" onclick=""&#10;&#9;&#9;&#9;&#9;&#9;&#9;&#9;&#9;&#9;&#9;if(Lightbox.managerDeletePermission()){&#10;&#9;&#9;&#9;&#9;&#9;&#9;&#9;&#9;&#9;&#9;&#9;zoomLightboxAction_onClick('46482','remove',function(){&#10;&#9;&#9;&#9;&#9;&#9;&#9;&#9;&#9;&#9;&#9;&#9;&#9;Lightbox.cwAlert(&quot;Removed from Lightbox&quot;,&quot;This image has been removed from your lightbox.&quot;,&quot;OK&quot;);&#10;&#9;&#9;&#9;&#9;&#9;&#9;&#9;&#9;&#9;&#9;&#9;});&#10;&#9;&#9;&#9;&#9;&#9;&#9;&#9;&#9;&#9;&#9;}&#10;&#9;&#9;&#9;&#9;&#9;&#9;&#9;&#9;&#9;&#9;else{&#10;&#9;&#9;&#9;&#9;&#9;&#9;&#9;&#9;&#9;&#9;&#9;Lightbox.showNoDeletePermissionWindow();   &#10;&#9;&#9;&#9;&#9;&#9;&#9;&#9;&#9;&#9;&#9;}&#10;&#9;&#9;&#9;&#9;&#9;&#9;&#9;&#9;&#9;"">Remove from lightbox</a>
              </li>
              <input type=""hidden"" name=""asset_id"" id=""asset_id"" value=""46482"" />
              <input type=""hidden"" name=""lightbox_id"" id=""lightbox_id"" value="""" />
              <input type=""hidden"" name=""price_item"" id=""price_item"" value="""" />
              <input type=""hidden"" name=""price_usage_code"" id=""price_usage_code"" value="""" />
              <input type=""hidden"" name=""price_ms_menus"" id=""price_ms_menus"" value="""" />
              <li>
                <a href=""JavaScript:window.print();"" class=""iconPreviewPrint"">Print preview with details</a>
              </li>
<!--DirectDownload0categoryDownloadGroup/allowBuycategoryDownloadGroup/viewOnlycategoryDownloadGroup/noPermissionaccessDirectDownloaddenySingleSell0featureId-->
              <li>
                <a class=""iconDownloadComp"" href=""&#10;&#9;&#9;&#9;&#9;&#9;&#9;&#9;&#9;&#9;&#9;&#9;&#9;javascript:Asset.downloadCompImage(46482, 2);&#10;&#9;&#9;&#9;&#9;&#9;&#9;&#9;&#9;&#9;&#9;&#9;"">Download lecture image</a>
              </li>
            </ul>
          </div>
          <div id=""Social-media"" style=""font-size:10px; padding-left:20px; width:180px;"">
            <a class=""fb-share-button"" id=""fbshare"" href=""https://www.facebook.com/sharer.php?u=http://images.nga.gov/en/search/do_quick_search.html?q=%7E46482%26launchZoom=46482"">
              <img alt=""Share on Facebook"" src=""https://images.nga.gov//images/fb-share.png"" />
            </a>
            <a id=""custom-tweet-button"" href=""http://twitter.com/share?url=http://images.nga.gov/en/search/do_quick_search.html?q=%7E46482%26launchZoom=46482"">
              <img alt=""Share on Twitter"" src=""https://images.nga.gov//images/Twitter_logo_blue_32.png"" />
            </a>
          </div>
          <div id=""info"" style=""font-size:10px; padding-left:0px; width:198px;"">
            <dl class=""info"">
              <dt>Artist</dt>
              <dd>Henri Rousseau</dd>
              <dt>Artist Info</dt>
              <dd>French, 1844 - 1910</dd>
              <dt>Title</dt>
              <dd>The Equatorial Jungle</dd>
              <dt>Dated</dt>
              <dd>1909</dd>
              <dt>Medium</dt>
              <dd>oil on canvas</dd>
              <dt>Classification</dt>
              <dd>Painting</dd>
              <dt>Dimensions</dt>
              <dd>overall: 140.6 x 129.5 cm (55 3/8 x 51 in.)  framed: 151.8 x 141.3 x 6.9 cm (59 3/4 x 55 5/8 x 2 11/16 in.)</dd>
              <dt>Credit</dt>
              <dd>Chester Dale Collection</dd>
              <dt>Accession No.</dt>
              <dd>1963.10.213</dd>
              <dt>Digitization</dt>
              <dd>Direct Digital Capture</dd>
              <dt>Image Use</dt>
              <dd>Open Access</dd>
            </dl>
          </div>
          <style>
					div#info ul,
					div#info ul li,
					div#info {
						font-size:11px;
					}
					div#info ul {
						padding-left:10px;
						list-style-type: disc;
					}
					div#info dl.info dt {
						width:85px;
					}
					div#info dl.info dd {
						width:94px;
					}
				</style>
          <div id=""info"">
            <a target=""_blank"" href=""http://www.nga.gov/purl/collection/artobject.html/46688"">
						View this object record on nga.gov
					</a>
          </div>
        </div>
        <div class=""right active"" id=""preview_right"" style=""float: left;"">
          <div id=""image"" style="""">
            <img alt=""preview"" style="""" src=""http://images.nga.gov/?service=asset&amp;action=show_preview&amp;asset=46482"" id=""imageimg"" />
          </div>
          <p class=""metadata"">
            <strong>Henri Rousseau</strong>
          </p>
          <p class=""metadata"">French, 1844 - 1910</p>
          <p class=""metadata"">The Equatorial Jungle</p>
          <p class=""metadata"">1909</p>
          <p class=""metadata"">overall: 140.6 x 129.5 cm (55 3/8 x 51 in.)  framed: 151.8 x 141.3 x 6.9 cm (59 3/4 x 55 5/8 x 2 11/16 in.)</p>
          <p class=""metadata"">Chester Dale Collection</p>
          <p class=""metadata"">1963.10.213</p>
        </div>
        <div class=""right"" id=""similars_right"" style=""display:none;float:left;"">You can easily find similar images by selecting any combination of the following keywords, concepts and image attributes.<br /><style>
					p.metadata {
					    display:none;
					}
					fieldset strong {
						width:115px;
						float:left;
						display:block;
						text-align:right;
						padding-right:5px;
					}
					fieldset legend {
						font-size:1.4em;
						font-weight:bold;
						margin-left: 15px;
					}
					fieldset label {
						display:block;
						float:left;
						font-size:1em;
						line-height:1em;
						font-weight:normal;
					}
					fieldset input {
						line-height:1em;
					}
					fieldset label:after {
						clear:both;
					}
					fieldset input.filesize {
						padding: 0px;
						font-size:0.9em;
						width: 50px;
						border-right-width: 2px;
						margin-left: 4px;	
					}
					fieldset input.date {
						padding: 0px;
						font-size:0.9em;
						width: 30px;
						border-right-width: 2px;
						margin-left: 4px;	
					}
					fieldset input.texts {
						padding: 0px;
						font-size:0.9em;
						width: 250px;
						border-right-width: 2px;
						margin-left: 4px;	
					}
					fieldset {
						color: #333333;
						border: 1px solid #999999;
						padding: 14px;
						margin-bottom: 15px;
					}
					fieldset legend {
						font-size: 12px;
						font-weight: bold;
						color: #333333;
					}
					fieldset a {
						color: #0066CC;
					}
					fieldset input {
						padding: 0px;
						margin-top: 0px;
						margin-right: 3px;
						margin-bottom: 0px;
						margin-left: -16px;
					}
					fieldset label {
						margin: 0px;
						padding: 0px 0px 0px 20px;
						width: 400px;
					}
					fieldset label {
						margin-right: 12px;
					}
					fieldset.keywords label {
						width:102px;
					}			
					fieldset.keywords p {
						margin:0;
					}
					fieldset br {
						line-height:1.9em;
					}	
					body.popup fieldset p {
						margin:0;
					}	
					body.popup fieldset div p {
						margin:0;    
						width:auto;
						display:inline;
						float:left;
					}
					body.popup fieldset div label{
						display:inline;
						width:auto;
						float:left;
						margin:0;
					}
					body.popup fieldset div.keywords{
						clear:both;
						width:511px;
						min-height:5px;
					}
					body.popup fieldset div.keywords label{
						display:inline;
						width:82px;
						overflow:hidden;
						float:left;
						margin:0;
					}
					body.popup fieldset div.row{
						clear:both;
					}
					body.popup fieldset div.row label{
						padding-left:0px;  
						line-height:1.5em;  
						width:120px;
						text-align:right;
					}
					body.popup fieldset div.row input{
						margin:0;
						margin-left:5px;
						padding:0;
						float:left;

					}
					body.popup fieldset div.row p{
						margin-left:5px;
						line-height:1.5em;
					}
					
					#similarsform .submitButtons input {
						margin-right: 5px;
					}
				</style><form method=""get"" target=""_parent"" id=""similarsform"" onsubmit=""&#10;&#9;&#9;&#9;&#9;&#9;&#9;var t = $('similarsform').serialize();d ='https://images.nga.gov/en/search/do_similar_search.html?'+t;window.opener.location.href=d;self.close();return false;&#10;&#9;&#9;&#9;&#9;&#9;"" action=""en/search/do_similar_search.html""><fieldset><legend>Image Attributes</legend><p>  </p><div class=""row""><label for=""md_3"">Artist</label><input type=""checkbox"" id=""md_3"" name=""md_3"" onclick=""$('submitSimilars').enable();$('resetSimilars').enable();"" value=""Rousseau, Henri"" /><p>Rousseau, Henri</p></div><div class=""row""><label for=""md_5"">Title</label><input type=""checkbox"" id=""md_5"" name=""md_5"" onclick=""$('submitSimilars').enable();$('resetSimilars').enable();"" value=""The Equatorial Jungle"" /><p>The Equatorial Jungle</p></div><div class=""row""><label for=""md_6"">Created</label><input type=""checkbox"" id=""md_6"" name=""md_6"" onclick=""$('submitSimilars').enable();$('resetSimilars').enable();"" value=""1909"" /><p>1909</p></div><div class=""row""><label for=""md_7"">Classification</label><input type=""checkbox"" id=""md_7"" name=""md_7"" onclick=""$('submitSimilars').enable();$('resetSimilars').enable();"" value=""Painting"" /><p>Painting</p></div></fieldset><div class=""submitButtons""><input type=""submit"" id=""submitSimilars"" disabled=""disabled"" value=""Submit"" /><input type=""reset"" id=""resetSimilars"" disabled=""disabled"" value=""Reset"" /></div></form></div>
      </div>
      <div id=""bottom"">
        <script type=""text/javascript"">
				$('imageimg').ondblclick  = function() {
					Popup.open({url:'en/asset/show_zoom_window_popup_img.html?asset=46482',width:this.width,height:this.height})
				}

				// Asset.updateBasketPreviewLink('46482');
				</script>
      </div>
    </div>
    <div id=""mainContent"" style=""display:none"" class=""""></div>
  </body>
</html>
";
            #endregion

            var parsed = AssetDetailsParser.ParseHtmlToNewModel(html);
            Assert.AreEqual("Henri Rousseau", parsed.OriginalArtist);
            Assert.AreEqual("henri rousseau", parsed.Artist);
            Assert.AreEqual("The Equatorial Jungle", parsed.Name);
            Assert.AreEqual("1909", parsed.Date);
            Assert.AreEqual("http://www.nga.gov/purl/collection/artobject.html/46688", parsed.SourceLink);
        }

    }
}
